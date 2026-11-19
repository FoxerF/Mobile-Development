
using MauiStartup.ViewModels;
using System.Threading;

namespace MauiStartup.Pages;

public partial class MovieDetailsPage : ContentPage
{
    private int _isClosing;

    public MovieDetailsPage(MovieDetailsViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
    }

    private async void OnCloseClicked(
        object sender,
        EventArgs e)
    {

        if (Interlocked.Exchange(ref _isClosing, 1) != 0)
            return;

        if (sender is Button button)
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
