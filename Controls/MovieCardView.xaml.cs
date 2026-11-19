
using System;
using System.Threading.Tasks;
using MauiStartup.Models;

namespace MauiStartup.Controls;

public partial class MovieCardView : ContentView
{
    private const uint AnimationDuration = 450;
    private const double InitialVerticalOffset = 40;
    private const int AnimationStaggerDelay = 50;

    private string? _lastAnimatedMovieId;

    private int _animationVersion;

    public static readonly BindableProperty ModelProperty =
        BindableProperty.Create(
            nameof(Model),
            typeof(Movie),
            typeof(MovieCardView),
            null,
            BindingMode.OneWay);

    public Movie? Model
    {
        get => (Movie?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    public MovieCardView()
    {
        InitializeComponent();

        Opacity = 0;
        TranslationY = InitialVerticalOffset;

        BindingContextChanged += HandleBindingContextChanged;
    }

    private void HandleBindingContextChanged(object? sender, EventArgs e)
    {
        if (BindingContext is not Movie movie)
            return;

        if (string.IsNullOrWhiteSpace(movie.Id))
            return;

        if (movie.Id == _lastAnimatedMovieId)
            return;

        _lastAnimatedMovieId = movie.Id;

        int animationVersion = ++_animationVersion;

        this.CancelAnimations();

        Opacity = 0;
        TranslationY = InitialVerticalOffset;

        _ = AnimateAppearingAsync(animationVersion);
    }

    private async Task AnimateAppearingAsync(int animationVersion)
    {
        try
        {
            await Task.Delay(AnimationStaggerDelay);

            if (animationVersion != _animationVersion)
                return;

            await Task.WhenAll(
                this.FadeToAsync(
                    1,
                    AnimationDuration,
                    Easing.CubicOut),

                this.TranslateToAsync(
                    0,
                    0,
                    AnimationDuration,
                    Easing.CubicOut));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Movie card animation failed: {ex}");

            if (animationVersion == _animationVersion)
            {
                Opacity = 1;
                TranslationY = 0;
            }
        }
    }
}
