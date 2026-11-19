
using MauiStartup.Services;

namespace MauiStartup.Pages;

public partial class InfoPage : ContentPage
{
    public InfoPage(AppStateService appStateService)
    {
        InitializeComponent();

        BindingContext = appStateService;
    }
}
