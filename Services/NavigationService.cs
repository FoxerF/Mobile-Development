using MauiStartup.Models;
using MauiStartup.Pages;
using MauiStartup.ViewModels;

namespace MauiStartup.Services;

public class NavigationService : INavigationService
{
    private readonly MovieService _movieService;

    public NavigationService(MovieService movieService)
    {
        _movieService = movieService;
    }

    public async Task ShowMovieDetailsAsync(Movie movie)
    {
        var viewModel = new MovieDetailsViewModel(
            movie,
            _movieService);

        var detailsPage =
            new MovieDetailsPage(viewModel);

        await Shell.Current.Navigation.PushModalAsync(
            detailsPage);
    }
}