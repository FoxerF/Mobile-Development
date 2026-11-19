using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MauiStartup.Pages;
using MauiStartup.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MauiStartup.Pages;

public partial class MovieCatalogPage : ContentPage
{
    private readonly MovieCatalogViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    private bool _isWideLayout;
    private int _isOpeningAddPage;

    public MovieCatalogPage(
        MovieCatalogViewModel viewModel,
        IServiceProvider serviceProvider)
    {
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;

        InitializeComponent();

        BindingContext = _viewModel;
    }

    public MovieCatalogViewModel ViewModel => _viewModel;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _viewModel.RefreshFavoritesView();

        await _viewModel.LoadInitialMoviesAsync();
    }

    private async void OnRemainingItemsThresholdReached(
        object sender,
        EventArgs e)
    {
        await _viewModel.LoadMoreMoviesAsync();
    }

    private async void OnAddMovieClicked(
        object sender,
        EventArgs e)
    {
        if (Interlocked.Exchange(ref _isOpeningAddPage, 1) != 0)
            return;

        try
        {
            var addMoviePage =
                _serviceProvider.GetRequiredService<AddMoviePage>();

            addMoviePage.Saved += (_, _) =>
            {
                _viewModel.RefreshAfterUserCreatedMovieSaved();
            };

            await Navigation.PushModalAsync(addMoviePage);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());

            await DisplayAlert(
                "Navigation error",
                ex.Message,
                "OK");
        }
        finally
        {
            Interlocked.Exchange(ref _isOpeningAddPage, 0);
        }
    }

    private void OnSwipeStarted(
        object sender,
        SwipeStartedEventArgs e)
    {
        MoviesCollection.SelectedItem = null;
        MoviesCollection.SelectionMode = SelectionMode.None;
    }

    private void OnSwipeEnded(
        object sender,
        SwipeEndedEventArgs e)
    {
        MoviesCollection.SelectionMode = SelectionMode.Single;
    }

    private void OnMoviesCollectionSizeChanged(
        object sender,
        EventArgs e)
    {
        if (sender is not CollectionView collectionView)
            return;

        if (collectionView.Width <= 0)
            return;

        bool useWideLayout = collectionView.Width >= 600;

        if (useWideLayout == _isWideLayout)
            return;

        _isWideLayout = useWideLayout;

        if (useWideLayout)
        {
            collectionView.ItemsLayout =
                new GridItemsLayout(
                    2,
                    ItemsLayoutOrientation.Vertical)
                {
                    VerticalItemSpacing = 12,
                    HorizontalItemSpacing = 12
                };
        }
        else
        {
            collectionView.ItemsLayout =
                new LinearItemsLayout(
                    ItemsLayoutOrientation.Vertical)
                {
                    ItemSpacing = 12
                };
        }
    }
}
