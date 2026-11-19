
using CommunityToolkit.Mvvm.ComponentModel;

namespace MauiStartup.Models;

public partial class Movie : ObservableObject
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int? ReleaseYear { get; set; }

    public string? PosterUrl { get; set; }

    public double? Rating { get; set; }

    public string Plot { get; set; } = "No description available.";

    [ObservableProperty]
    private bool isFavorite;

    [ObservableProperty]
    private bool isUserCreated;
}
