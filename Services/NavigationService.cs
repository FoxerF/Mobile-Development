
using MauiStartup.Models;
using MauiStartup.Pages;
using MauiStartup.ViewModels;

namespace MauiStartup.Services;

public class NavigationService : INavigationService
{
    private readonly FavoriteMoviesService _favoriteMoviesService;

    public NavigationService(
        FavoriteMoviesService favoriteMoviesService)
    {
        _favoriteMoviesService = favoriteMoviesService;
    }

    public async Task ShowMovieDetailsAsync(Movie movie)
    {
        var viewModel = new MovieDetailsViewModel(
            movie,
            _favoriteMoviesService);

        var detailsPage = new MovieDetailsPage(viewModel);

        await Shell.Current.Navigation.PushModalAsync(detailsPage);
    }
}
