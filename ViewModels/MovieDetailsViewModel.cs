
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStartup.Models;
using MauiStartup.Services;

namespace MauiStartup.ViewModels;

public partial class MovieDetailsViewModel : ObservableObject
{
    private readonly FavoriteMoviesService _favoriteMoviesService;

    [ObservableProperty]
    private Movie movie;

    public MovieDetailsViewModel(
        Movie movie,
        FavoriteMoviesService favoriteMoviesService)
    {
        this.movie = movie;
        _favoriteMoviesService = favoriteMoviesService;
    }

    [RelayCommand]
    private Task RemoveFromFavoritesAsync()
    {
        _favoriteMoviesService.Remove(Movie);

        return Task.CompletedTask;
    }
}
