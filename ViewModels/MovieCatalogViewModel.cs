using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    private readonly MovieService _movieService;

    private readonly INavigationService _navigationService;

    private const int VisibleBatchSize = 10;

    public ObservableCollection<Movie> TitlesLoaded =>
        _movieService.ApiMovies;

    public ObservableCollection<Movie> UserCreatedMovies =>
        _movieService.UserCreatedMovies;

    [ObservableProperty]
    private ObservableCollection<Movie> titlesVisible = new();

    private int _visibleMovieCount;

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
        MovieService movieService,
        INavigationService navigationService)
    {
        _movieService = movieService;
        _navigationService = navigationService;

        _movieService.UserCreatedMovies.CollectionChanged +=
            OnUserCreatedMoviesChanged;
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
            await _movieService.InitializeAsync();

            _nextPage =
                await _movieService.LoadApiMoviesAsync(1);

            _hasLoadedInitialPage = true;

            _visibleMovieCount = 0;

            TitlesVisible =
                new ObservableCollection<Movie>();

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
            _nextPage =
                await _movieService.LoadApiMoviesAsync(
                    _nextPage.Value);

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

    private void RevealNextBatch()
    {
        int endIndex = Math.Min(
            _visibleMovieCount + VisibleBatchSize,
            TitlesLoaded.Count);

        while (_visibleMovieCount < endIndex)
        {
            TitlesVisible.Add(
                TitlesLoaded[_visibleMovieCount]);

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

        TitlesVisible = _movieService.Favorites;

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

        TitlesVisible = _movieService.UserCreatedMovies;

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

    public void RefreshAfterUserCreatedMovieSaved()
    {
        ShowAllMovies();
    }

    private void OnUserCreatedMoviesChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        if (IsDisplayingFavorites ||
            IsDisplayingUserMovies)
        {
            UpdateHasMoreItems();
            return;
        }

        if (e.OldItems is not null)
        {
            foreach (Movie removedMovie in e.OldItems)
            {
                Movie? visibleMovie =
                    TitlesVisible.FirstOrDefault(
                        existing => existing.Id == removedMovie.Id);

                if (visibleMovie is not null)
                    TitlesVisible.Remove(visibleMovie);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (Movie addedMovie in e.NewItems)
            {
                if (!TitlesVisible.Any(
                    existing => existing.Id == addedMovie.Id))
                {
                    TitlesVisible.Add(addedMovie);
                }
            }
        }

        UpdateHasMoreItems();
    }

    [RelayCommand]
    private Task ManageFavoritesAsync(Movie? movie)
    {
        if (movie is null)
            return Task.CompletedTask;

        if (IsDisplayingFavorites)
        {
            _movieService.RemoveFromFavorites(movie);
        }
        else
        {
            _movieService.AddToFavorites(movie);
        }

        return Task.CompletedTask;
    }

    public void RefreshFavoritesView()
    {
        if (!IsDisplayingFavorites)
            return;

        TitlesVisible = _movieService.Favorites;

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

        ErrorMessage =
            $"{ex.GetType().Name}: {ex.Message}";

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