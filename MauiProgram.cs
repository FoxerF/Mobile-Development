
using MauiStartup.Services;
using MauiStartup.ViewModels;
using MauiStartup.Pages;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace MauiStartup;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<AppStateService>();

        builder.Services.AddSingleton<AuthService>(_ =>
            new AuthService(new HttpClient()));

        builder.Services.AddSingleton<MainPageModel>();

        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<InfoPage>();

        builder.Services.AddSingleton<HttpClient>(_ =>
        {
            var client = new HttpClient
            {
                BaseAddress =
                    new Uri("https://api.themoviedb.org/3/")
            };

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    TmdbSecrets.ReadAccessToken
                );

            return client;
        });

        builder.Services.AddSingleton<MovieApiService>();

        builder.Services.AddSingleton<StorageService>();
        builder.Services.AddSingleton<MovieService>();

        builder.Services.AddSingleton<MovieCatalogViewModel>();
        builder.Services.AddSingleton<MovieCatalogPage>();

        builder.Services.AddTransient<MovieFormViewModel>();
        builder.Services.AddTransient<AddMoviePage>();

        builder.Services.AddSingleton<AppShell>();

        builder.Services.AddSingleton<
            INavigationService,
            NavigationService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
