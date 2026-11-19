
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;
using MauiStartup.Models;
using MauiStartup.Services;

namespace MauiStartup.ViewModels;

public partial class MovieCatalogViewModel : ObservableObject
{
    private readonly MovieApiService _movieApiService;
    private readonly INavigationService _navigationService;
    private readonly FavoriteMoviesService _favoriteMoviesService;

    private const int VisibleBatchSize = 10;

    public ObservableCollection<Movie> TitlesLoaded { get; } = new();

    public ObservableCollection<Movie> UserCreatedMovies { get; } = new();

    [ObservableProperty]
    private ObservableCollection<Movie> titlesVisible = new();

    private int _visibleMovieCount;

    private readonly HashSet<string> _loadedMovieIds =
        new(StringComparer.Ordinal);

    private int? _nextPage;
    private bool _hasLoadedInitialPage;
    private bool _paginationPaused;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private Movie? selectedMovie;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isErrorVisible;

    [ObservableProperty]
    private bool hasMoreItems = true;

    [ObservableProperty]
    private bool isDisplayingFavorites;

    [ObservableProperty]
    private bool isDisplayingUserMovies;

    [ObservableProperty]
    private string catalogTitle = "Популярні фільми";

    [ObservableProperty]
    private string swipeText = "Favorite";

    [ObservableProperty]
    private Color swipeBackground = Colors.Gold;

    [ObservableProperty]
    private string favoritesButtonText = "Show Favorites";

    [ObservableProperty]
    private Color favoritesButtonBackground = Colors.SteelBlue;

    [ObservableProperty]
    private string userMoviesButtonText = "Show My Movies";

    [ObservableProperty]
    private Color userMoviesButtonBackground = Colors.DarkSlateBlue;

    [ObservableProperty]
    private int remainingItemsThreshold = 5;

    public MovieCatalogViewModel(
        MovieApiService movieApiService,
        INavigationService navigationService,
        FavoriteMoviesService favoriteMoviesService)
    {
        _movieApiService = movieApiService;
        _navigationService = navigationService;
        _favoriteMoviesService = favoriteMoviesService;
    }

    public async Task LoadInitialMoviesAsync()
    {
        if (IsLoading || _hasLoadedInitialPage)
            return;

        IsLoading = true;
        IsErrorVisible = false;
        ErrorMessage = string.Empty;

        try
        {
            var result =
                await _movieApiService.GetMoviesAsync(page: 1);

            TitlesLoaded.Clear();
            TitlesVisible.Clear();
            _loadedMovieIds.Clear();

            _visibleMovieCount = 0;

            AddLoadedMovies(result.Movies);

            _nextPage = result.NextPage;

            _hasLoadedInitialPage = true;

            RevealNextBatch();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadMoreMoviesAsync()
    {
        if (IsDisplayingFavorites ||
            IsDisplayingUserMovies ||
            IsLoading ||
            !_hasLoadedInitialPage ||
            !HasMoreItems ||
            _paginationPaused)
        {
            return;
        }

        IsErrorVisible = false;
        ErrorMessage = string.Empty;

        if (_visibleMovieCount < TitlesLoaded.Count)
        {
            RevealNextBatch();
            return;
        }

        if (!_nextPage.HasValue)
        {
            UpdateHasMoreItems();
            return;
        }

        IsLoading = true;

        try
        {
            var result =
                await _movieApiService.GetMoviesAsync(
                    page: _nextPage.Value);

            AddLoadedMovies(result.Movies);

            _nextPage = result.NextPage;

            RevealNextBatch();
        }
        catch (Exception ex)
        {
            _paginationPaused = true;
            ShowError(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddLoadedMovies(IEnumerable<Movie> movies)
    {
        foreach (Movie movie in movies)
        {
            if (string.IsNullOrWhiteSpace(movie.Id) ||
                _loadedMovieIds.Add(movie.Id))
            {
                TitlesLoaded.Add(movie);
            }
        }
    }

    private void RevealNextBatch()
    {
        int endIndex = Math.Min(
            _visibleMovieCount + VisibleBatchSize,
            TitlesLoaded.Count);

        while (_visibleMovieCount < endIndex)
        {
            TitlesVisible.Add(TitlesLoaded[_visibleMovieCount]);
            _visibleMovieCount++;
        }

        foreach (Movie movie in UserCreatedMovies)
        {
            if (!TitlesVisible.Any(
                existing => existing.Id == movie.Id))
            {
                TitlesVisible.Add(movie);
            }
        }

        UpdateHasMoreItems();
    }

    private void UpdateHasMoreItems()
    {
        HasMoreItems =
            !IsDisplayingFavorites &&
            !IsDisplayingUserMovies &&
            (_visibleMovieCount < TitlesLoaded.Count ||
             _nextPage.HasValue);
    }

    [RelayCommand]
    private Task ToggleFavoritesAsync()
    {
        SelectedMovie = null;

        IsErrorVisible = false;
        ErrorMessage = string.Empty;

        if (IsDisplayingFavorites)
        {
            ShowAllMovies();
            return Task.CompletedTask;
        }

        IsDisplayingFavorites = true;
        IsDisplayingUserMovies = false;

        TitlesVisible =
            new ObservableCollection<Movie>(
                _favoriteMoviesService.Favorites);

        CatalogTitle = "Улюблені фільми";

        SwipeText = "Delete";
        SwipeBackground = Colors.Red;

        FavoritesButtonText = "Show All Movies";
        FavoritesButtonBackground = Colors.Goldenrod;

        UserMoviesButtonText = "Show My Movies";
        UserMoviesButtonBackground = Colors.DarkSlateBlue;

        RemainingItemsThreshold = -1;

        UpdateHasMoreItems();

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ToggleUserMoviesAsync()
    {
        SelectedMovie = null;

        IsErrorVisible = false;
        ErrorMessage = string.Empty;

        if (IsDisplayingUserMovies)
        {
            ShowAllMovies();
            return Task.CompletedTask;
        }

        IsDisplayingUserMovies = true;
        IsDisplayingFavorites = false;

        TitlesVisible =
            new ObservableCollection<Movie>(
                UserCreatedMovies);

        CatalogTitle = "Мої фільми";

        SwipeText = "Favorite";
        SwipeBackground = Colors.Gold;

        FavoritesButtonText = "Show Favorites";
        FavoritesButtonBackground = Colors.SteelBlue;

        UserMoviesButtonText = "Show All Movies";
        UserMoviesButtonBackground = Colors.Goldenrod;

        RemainingItemsThreshold = -1;

        UpdateHasMoreItems();

        return Task.CompletedTask;
    }

    private void ShowAllMovies()
    {
        IsDisplayingFavorites = false;
        IsDisplayingUserMovies = false;

        SelectedMovie = null;

        TitlesVisible =
            new ObservableCollection<Movie>(
                TitlesLoaded
                    .Take(_visibleMovieCount)
                    .Concat(UserCreatedMovies));

        CatalogTitle = "Популярні фільми";

        SwipeText = "Favorite";
        SwipeBackground = Colors.Gold;

        FavoritesButtonText = "Show Favorites";
        FavoritesButtonBackground = Colors.SteelBlue;

        UserMoviesButtonText = "Show My Movies";
        UserMoviesButtonBackground = Colors.DarkSlateBlue;

        RemainingItemsThreshold = 5;

        UpdateHasMoreItems();
    }

    public void AddUserCreatedMovie(Movie movie)
    {
        if (UserCreatedMovies.Any(
            existing => existing.Id == movie.Id))
        {
            return;
        }

        movie.IsUserCreated = true;

        UserCreatedMovies.Add(movie);

        ShowAllMovies();
    }

    [RelayCommand]
    private Task ManageFavoritesAsync(Movie? movie)
    {
        if (movie is null)
            return Task.CompletedTask;

        if (IsDisplayingFavorites)
        {
            _favoriteMoviesService.Remove(movie);

            Movie? visibleMovie = TitlesVisible.FirstOrDefault(
                existing => existing.Id == movie.Id);

            if (visibleMovie is not null)
                TitlesVisible.Remove(visibleMovie);
        }
        else
        {
            _favoriteMoviesService.Add(movie);
        }

        return Task.CompletedTask;
    }

    public void RefreshFavoritesView()
    {
        if (!IsDisplayingFavorites)
            return;

        TitlesVisible =
            new ObservableCollection<Movie>(
                _favoriteMoviesService.Favorites);

        UpdateHasMoreItems();
    }

    [RelayCommand]
    private async Task RetryLoadingAsync()
    {
        if (IsDisplayingFavorites ||
            IsDisplayingUserMovies)
        {
            return;
        }

        if (_hasLoadedInitialPage)
        {
            _paginationPaused = false;
            await LoadMoreMoviesAsync();
        }
        else
        {
            await LoadInitialMoviesAsync();
        }
    }

    private void ShowError(Exception ex)
    {
        System.Diagnostics.Debug.WriteLine(ex.ToString());

        ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";

        if (ex.InnerException != null)
        {
            ErrorMessage +=
                $"\nInner exception: {ex.InnerException.Message}";
        }

        IsErrorVisible = true;
    }

    [RelayCommand]
    private async Task OpenMovieDetailsAsync(Movie? movie)
    {
        if (movie is null)
            return;

        await _navigationService.ShowMovieDetailsAsync(movie);

        SelectedMovie = null;
    }
}
