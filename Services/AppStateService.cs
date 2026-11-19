
using CommunityToolkit.Mvvm.ComponentModel;

namespace MauiStartup.Services;

public partial class AppStateService : ObservableObject
{
    [ObservableProperty]
    private bool isRegistered;

    [ObservableProperty]
    private string registeredEmail = string.Empty;

    public bool IsNotRegistered => !IsRegistered;

    partial void OnIsRegisteredChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotRegistered));
    }

    public void MarkRegistered(string email)
    {
        RegisteredEmail = email;
        IsRegistered = true;
    }
}
