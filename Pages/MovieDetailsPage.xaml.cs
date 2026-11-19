using MauiStartup.ViewModels;
using System.Threading;

namespace MauiStartup.Pages;

public partial class MovieDetailsPage : ContentPage
{
    private int _isClosing;

    private readonly MovieDetailsViewModel _viewModel;

    public MovieDetailsPage(MovieDetailsViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;

        BindingContext = _viewModel;

        _viewModel.UserMovieDeleted += OnUserMovieDeleted;
    }
    private async void OnUserMovieDeleted(
    object? sender,
    EventArgs e)
    {
        await CloseModalAsync();
    }
    private async void OnCloseClicked(
        object sender,
        EventArgs e)
    {
        await CloseModalAsync(sender as Button);
    }

    private async Task CloseModalAsync(Button? button = null)
    {
        if (Interlocked.Exchange(ref _isClosing, 1) != 0)
            return;

        if (button is not null)
            button.IsEnabled = false;

        try
        {
            var modalStack = Navigation.ModalStack;

            if (modalStack.Count > 0 &&
                ReferenceEquals(modalStack[^1], this))
            {
                await Navigation.PopModalAsync();
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}
