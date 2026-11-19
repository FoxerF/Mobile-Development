using System;
using System.Threading;
using System.Threading.Tasks;
using MauiStartup.Models;
using MauiStartup.ViewModels;

namespace MauiStartup.Pages;

public partial class AddMoviePage : ContentPage
{
    private int _isClosing;

    private readonly MovieFormViewModel _viewModel;

    public event EventHandler<Movie>? Saved;

    public AddMoviePage(MovieFormViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.Saved += OnMovieSaved;
    }

    private async void OnMovieSaved(
        object? sender,
        Movie movie)
    {
        Saved?.Invoke(this, movie);

        await CloseModalAsync();
    }

    private async void OnCancelClicked(
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
