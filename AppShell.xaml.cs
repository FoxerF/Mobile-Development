
using MauiStartup.Pages;

namespace MauiStartup;

public partial class AppShell : Shell
{
    public AppShell(
    MainPage mainPage,
    InfoPage infoPage,
    MovieCatalogPage movieCatalogPage)
    {
        InitializeComponent();

        var mainTab = new Tab
        {
            Title = "Головна"
        };

        mainTab.Items.Add(new ShellContent
        {
            Title = "Реєстрація",
            Route = "main",
            Content = mainPage
        });

        var mainFlyoutItem = new FlyoutItem
        {
            Title = "Головна",
            Route = "home"
        };

        mainFlyoutItem.Items.Add(mainTab);

        var infoTab = new Tab
        {
            Title = "Інформація"
        };

        infoTab.Items.Add(new ShellContent
        {
            Title = "Інформація",
            Route = "info",
            Content = infoPage
        });

        var infoFlyoutItem = new FlyoutItem
        {
            Title = "Інформація",
            Route = "information"
        };

        infoFlyoutItem.Items.Add(infoTab);

        Items.Add(mainFlyoutItem);
        Items.Add(infoFlyoutItem);
        var moviesItem = new FlyoutItem
        {
            Title = "Каталог фільмів"
        };

        moviesItem.Items.Add(new Tab
        {
            Title = "Фільми",
            Items =
    {
        new ShellContent
        {
            Title = "Каталог",
            Content = movieCatalogPage
        }
    }
        });

        Items.Add(moviesItem);
    }
}
