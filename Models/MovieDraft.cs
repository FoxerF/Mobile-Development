
using CommunityToolkit.Mvvm.ComponentModel;

namespace MauiStartup.Models;

public partial class MovieDraft : ObservableObject
{
    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private int? releaseYear;

    [ObservableProperty]
    private string plot = string.Empty;

    [ObservableProperty]
    private string? posterPath;
}
