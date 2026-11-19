
using System.Collections.ObjectModel;
using System.Linq;
using MauiStartup.Models;

namespace MauiStartup.Services;

public class FavoriteMoviesService
{
    public ObservableCollection<Movie> Favorites { get; } = new();

    public bool Contains(string movieId)
    {
        return Favorites.Any(movie => movie.Id == movieId);
    }

    public bool Add(Movie movie)
    {
        if (string.IsNullOrWhiteSpace(movie.Id) ||
            Contains(movie.Id))
        {
            return false;
        }

        Favorites.Add(movie);
        movie.IsFavorite = true;

        return true;
    }

    public bool Remove(Movie movie)
    {
        Movie? existing = Favorites.FirstOrDefault(
            favorite => favorite.Id == movie.Id);

        if (existing is null)
            return false;

        Favorites.Remove(existing);

        existing.IsFavorite = false;
        movie.IsFavorite = false;

        return true;
    }
}
