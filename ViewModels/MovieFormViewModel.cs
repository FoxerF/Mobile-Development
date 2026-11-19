using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStartup.Models;
using Microsoft.Maui.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MauiStartup.ViewModels;

public partial class MovieFormViewModel : ObservableObject
{
    [ObservableProperty]
    private MovieDraft draft = new();

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isErrorVisible;

    [ObservableProperty]
    private bool isBusy;

    public event EventHandler<Movie>? Saved;

    [RelayCommand]
    private async Task SelectPosterAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        IsErrorVisible = false;

        try
        {
            var result = await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle = "Select a movie poster",
                    FileTypes = FilePickerFileType.Images
                });

            if (result is null)
                return;

            string posterDirectory = Path.Combine(
                FileSystem.AppDataDirectory,
                "UserMoviePosters");

            Directory.CreateDirectory(posterDirectory);

            string extension = Path.GetExtension(result.FileName);

            if (string.IsNullOrWhiteSpace(extension) ||
                extension.Length > 10)
            {
                extension = ".jpg";
            }

            string destinationPath = Path.Combine(
                posterDirectory,
                $"{Guid.NewGuid():N}{extension}");

            await using (Stream source = await result.OpenReadAsync())
            {
                await using FileStream destination =
                    File.Create(destinationPath);

                await source.CopyToAsync(destination);
            }

            Draft.PosterPath = destinationPath;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ShowError($"Could not select the poster: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task SaveAsync()
    {
        IsErrorVisible = false;

        if (string.IsNullOrWhiteSpace(Draft.Title))
        {
            ShowError("Please enter a movie title.");
            return Task.CompletedTask;
        }

        if (!Draft.ReleaseYear.HasValue ||
            Draft.ReleaseYear.Value < 1888 ||
            Draft.ReleaseYear.Value > DateTime.Now.Year + 10)
        {
            ShowError("Please enter a valid release year.");
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(Draft.PosterPath) ||
            !File.Exists(Draft.PosterPath))
        {
            ShowError("Please select a valid movie poster.");
            return Task.CompletedTask;
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

        Saved?.Invoke(this, movie);

        return Task.CompletedTask;
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        IsErrorVisible = true;
    }
}
