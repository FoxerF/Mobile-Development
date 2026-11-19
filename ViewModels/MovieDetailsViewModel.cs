using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStartup.Models;
using MauiStartup.Services;

namespace MauiStartup.ViewModels;

public partial class MovieDetailsViewModel : ObservableObject
{
    private readonly MovieService _movieService;

    [ObservableProperty]
    private Movie movie;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isErrorVisible;

    public event EventHandler? UserMovieDeleted;

    public MovieDetailsViewModel(
        Movie movie,
        MovieService movieService)
    {
        this.movie = movie;

        _movieService = movieService;
    }

    [RelayCommand]
    private Task RemoveFromFavoritesAsync()
    {
        _movieService.RemoveFromFavorites(Movie);

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task DeleteUserCreatedMovieAsync()
    {
        if (!Movie.IsUserCreated)
            return;

        IsErrorVisible = false;
        ErrorMessage = string.Empty;

        try
        {
            bool deleted =
                await _movieService.DeleteUserCreatedMovieAsync(
                    Movie);

            if (!deleted)
            {
                ShowError(
                    "This movie was not found in saved storage.");

                return;
            }

            UserMovieDeleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ShowError(
                $"Could not delete the movie: {ex.Message}");
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        IsErrorVisible = true;
    }
}