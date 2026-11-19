
using System;
using System.Collections.Generic;
using System.Net.Mail;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStartup.Services;

namespace MauiStartup.ViewModels;

public partial class MainPageModel : ObservableObject
{
    private readonly AppStateService _appStateService;

    private readonly Dictionary<string, string> _registeredUsers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["foxer@example.com"] = "Maui123!",
            ["student@example.com"] = "Study123!"
        };

    public MainPageModel(AppStateService appStateService)
    {
        _appStateService = appStateService;
    }

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string resultMessage = string.Empty;

    [ObservableProperty]
    private bool isResultVisible;

    [RelayCommand]
    private void CheckRegistration()
    {
        string emailInput = Email?.Trim() ?? string.Empty;
        string passwordInput = Password ?? string.Empty;

        if (!MailAddress.TryCreate(emailInput, out MailAddress? parsedEmail) ||
            !string.Equals(
                parsedEmail.Address,
                emailInput,
                StringComparison.OrdinalIgnoreCase))
        {
            ResultMessage = "Некоректний формат електронної пошти.";
            IsResultVisible = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(passwordInput))
        {
            ResultMessage = "Введіть пароль.";
            IsResultVisible = true;
            return;
        }

        bool isRegistered =
            _registeredUsers.TryGetValue(emailInput, out string? storedPassword) &&
            string.Equals(
                storedPassword,
                passwordInput,
                StringComparison.Ordinal);

        if (isRegistered)
        {
            _appStateService.MarkRegistered(emailInput);

            ResultMessage = "Реєстрацію підтверджено!";
        }
        else
        {
            ResultMessage = "Користувача з такими даними не знайдено.";
        }

        IsResultVisible = true;
    }

    partial void OnEmailChanged(string value)
    {
        ClearResult();
    }

    partial void OnPasswordChanged(string value)
    {
        ClearResult();
    }

    private void ClearResult()
    {
        ResultMessage = string.Empty;
        IsResultVisible = false;
    }
}
