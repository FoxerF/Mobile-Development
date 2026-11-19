using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStartup.Models;
using MauiStartup.Services;
using Microsoft.Maui.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MauiStartup.ViewModels;

public partial class MovieFormViewModel : ObservableObject
{
    private readonly MovieService _movieService;

    private bool _posterCommitted;

    [ObservableProperty]
    private MovieDraft draft = new();

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isErrorVisible;

    [ObservableProperty]
    private bool isBusy;

    public event EventHandler<Movie>? Saved;

    public MovieFormViewModel(MovieService movieService)
    {
        _movieService = movieService;
    }

    [RelayCommand]
    private async Task SelectPosterAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        IsErrorVisible = false;

        try
        {
            var result =
                await FilePicker.Default.PickAsync(
                    new PickOptions
                    {
                        PickerTitle = "Select a movie poster",
                        FileTypes = FilePickerFileType.Images
                    });

            if (result is null)
                return;

            string? previousPosterPath = Draft.PosterPath;

            string newPosterPath =
                await _movieService.CopyPosterAsync(result);

            Draft.PosterPath = newPosterPath;

            if (!string.IsNullOrWhiteSpace(previousPosterPath))
            {
                try
                {
                    await _movieService.DeleteUncommittedPosterAsync(
                        previousPosterPath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ShowError(
                $"Could not select the poster: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy)
            return;

        IsErrorVisible = false;

        if (string.IsNullOrWhiteSpace(Draft.Title))
        {
            ShowError("Please enter a movie title.");
            return;
        }

        if (!Draft.ReleaseYear.HasValue ||
            Draft.ReleaseYear.Value < 1888 ||
            Draft.ReleaseYear.Value > DateTime.Now.Year + 10)
        {
            ShowError("Please enter a valid release year.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Draft.PosterPath) ||
            !File.Exists(Draft.PosterPath))
        {
            ShowError("Please select a valid movie poster.");
            return;
        }

        var movie = new Movie
        {
            Id = $"user-{Guid.NewGuid():N}",

            Title = Draft.Title.Trim(),

            ReleaseYear = Draft.ReleaseYear,

            PosterUrl = Draft.PosterPath,

            Rating = null,

            Plot = string.IsNullOrWhiteSpace(Draft.Plot)
                ? "No description available."
                : Draft.Plot.Trim(),

            IsFavorite = false,

            IsUserCreated = true
        };

        IsBusy = true;

        try
        {
            await _movieService.AddUserCreatedMovieAsync(movie);

            _posterCommitted = true;

            Saved?.Invoke(this, movie);
        }
        catch (Exception ex)
        {
            ShowError(
                $"Could not save the movie: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task DiscardSelectedPosterAsync()
    {
        if (_posterCommitted ||
            string.IsNullOrWhiteSpace(Draft.PosterPath))
        {
            return;
        }

        string path = Draft.PosterPath;

        Draft.PosterPath = null;

        try
        {
            await _movieService.DeleteUncommittedPosterAsync(path);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        IsErrorVisible = true;
    }
}