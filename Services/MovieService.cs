using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MauiStartup.Models;
using Microsoft.Maui.Storage;

namespace MauiStartup.Services;

public class MovieService
{
    private readonly MovieApiService _movieApiService;

    private readonly StorageService _storageService;

    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    private readonly HashSet<string> _apiMovieIds =
        new(StringComparer.Ordinal);

    private bool _isInitialized;

    public ObservableCollection<Movie> ApiMovies { get; } = new();

    public ObservableCollection<Movie> UserCreatedMovies { get; } = new();

    public ObservableCollection<Movie> Favorites { get; } = new();

    public MovieService(
        MovieApiService movieApiService,
        StorageService storageService)
    {
        _movieApiService = movieApiService;
        _storageService = storageService;
    }

    public async Task InitializeAsync()
    {
        await _initializationLock.WaitAsync();

        try
        {
            if (_isInitialized)
                return;

            var savedMovies =
                await _storageService.LoadMoviesAsync();

            foreach (Movie movie in savedMovies)
            {
                if (string.IsNullOrWhiteSpace(movie.Id))
                    continue;

                movie.IsUserCreated = true;
                movie.IsFavorite = false;

                UserCreatedMovies.Add(movie);
            }

            _isInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task<int?> LoadApiMoviesAsync(int page)
    {
        await InitializeAsync();

        var result =
            await _movieApiService.GetMoviesAsync(page);

        foreach (Movie movie in result.Movies)
        {
            if (string.IsNullOrWhiteSpace(movie.Id))
                continue;

            if (_apiMovieIds.Add(movie.Id))
                ApiMovies.Add(movie);
        }

        return result.NextPage;
    }

    public Task<string> CopyPosterAsync(FileResult file)
    {
        return _storageService.CopyPosterAsync(file);
    }

    public Task DeleteUncommittedPosterAsync(string? path)
    {
        return _storageService.DeletePosterAsync(path);
    }

    public async Task AddUserCreatedMovieAsync(Movie movie)
    {
        await InitializeAsync();

        if (string.IsNullOrWhiteSpace(movie.Id))
            throw new InvalidOperationException(
                "The movie must have an ID.");

        if (!movie.IsUserCreated)
            throw new InvalidOperationException(
                "The movie must be marked as user-created.");

        if (string.IsNullOrWhiteSpace(movie.PosterUrl) ||
            !File.Exists(movie.PosterUrl))
        {
            throw new FileNotFoundException(
                "The movie poster does not exist.");
        }

        if (UserCreatedMovies.Any(
            existing => existing.Id == movie.Id))
        {
            return;
        }

        List<Movie> updatedMovies =
            UserCreatedMovies
                .Append(movie)
                .ToList();

        await _storageService.SaveMoviesAsync(updatedMovies);

        UserCreatedMovies.Add(movie);
    }

    public async Task<bool> DeleteUserCreatedMovieAsync(
        Movie movie)
    {
        await InitializeAsync();

        if (!movie.IsUserCreated)
            return false;

        Movie? existingMovie =
            UserCreatedMovies.FirstOrDefault(
                existing => existing.Id == movie.Id);

        if (existingMovie is null)
            return false;

        List<Movie> updatedMovies =
            UserCreatedMovies
                .Where(existing => existing.Id != movie.Id)
                .ToList();

        await _storageService.SaveMoviesAsync(updatedMovies);

        UserCreatedMovies.Remove(existingMovie);

        RemoveFromFavorites(existingMovie);

        try
        {
            await _storageService.DeletePosterAsync(
                existingMovie.PosterUrl);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Poster deletion failed: {ex}");
        }

        return true;
    }

    public bool AddToFavorites(Movie movie)
    {
        if (string.IsNullOrWhiteSpace(movie.Id) ||
            Favorites.Any(
                existing => existing.Id == movie.Id))
        {
            return false;
        }

        Favorites.Add(movie);
        movie.IsFavorite = true;

        return true;
    }

    public bool RemoveFromFavorites(Movie movie)
    {
        Movie? existing =
            Favorites.FirstOrDefault(
                favorite => favorite.Id == movie.Id);

        if (existing is null)
            return false;

        Favorites.Remove(existing);

        existing.IsFavorite = false;
        movie.IsFavorite = false;

        return true;
    }
}