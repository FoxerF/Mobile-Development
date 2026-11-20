using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStartup.Models;
using MauiStartup.Services;

namespace MauiStartup.ViewModels;

public partial class MainPageModel : ObservableObject
{
    private readonly AuthService _authService;

    private bool _initialized;

    [ObservableProperty]
    private AuthUser? currentUser;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public bool IsAuthenticated =>
        CurrentUser is not null;

    public bool IsNotAuthenticated =>
        CurrentUser is null;

    public MainPageModel(AuthService authService)
    {
        _authService = authService;

        CurrentUser = _authService.CurrentUser;

        _authService.PropertyChanged +=
            OnAuthServicePropertyChanged;
    }

    private void OnAuthServicePropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName ==
            nameof(AuthService.CurrentUser))
        {
            CurrentUser = _authService.CurrentUser;
        }
    }

    partial void OnCurrentUserChanged(AuthUser? value)
    {
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(IsNotAuthenticated));
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;

        try
        {
            await _authService.RestoreSessionAsync();
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Session restoration failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            AuthUser user =
                await _authService.SignInAsync();

            CurrentUser = user;

            StatusMessage =
                $"Successfully signed in as {user.Email}.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Sign-in was cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Sign-in failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;

        try
        {
            await _authService.SignOutAsync();

            CurrentUser = null;

            StatusMessage = "You have signed out.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Sign-out failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
