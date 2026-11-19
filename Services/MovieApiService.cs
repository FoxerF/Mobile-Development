
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MauiStartup.Models;

namespace MauiStartup.Services;

public class MovieApiService
{
    private readonly HttpClient _httpClient;

    private const string PosterBaseUrl =
        "https://image.tmdb.org/t/p/w500";

    public MovieApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(List<Movie> Movies, int? NextPage)>
        GetMoviesAsync(
            int page = 1,
            CancellationToken cancellationToken = default)
    {
        string requestUri =
            $"movie/popular?language=en-US&page={page}";

        using HttpResponseMessage response =
            await _httpClient.GetAsync(
                requestUri,
                cancellationToken);

        string json =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"TMDB returned {(int)response.StatusCode} " +
                $"{response.ReasonPhrase}. Response: {json}");
        }

        using JsonDocument document = JsonDocument.Parse(json);

        JsonElement root = document.RootElement;

        if (!root.TryGetProperty(
                "results", out JsonElement resultsElement) ||
            resultsElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                "The TMDB response did not contain a valid results array.");
        }

        var movies = new List<Movie>();

        foreach (JsonElement item in resultsElement.EnumerateArray())
        {
            string? title = GetString(item, "title");

            if (string.IsNullOrWhiteSpace(title))
                continue;

            string? posterPath = GetString(item, "poster_path");
            string? plot = GetString(item, "overview");

            movies.Add(new Movie
            {
                Id = GetInt32(item, "id")?.ToString(
                    CultureInfo.InvariantCulture) ?? string.Empty,

                Title = title,

                ReleaseYear = GetReleaseYear(item),

                PosterUrl = GetPosterUrl(posterPath),

                Rating = GetDouble(item, "vote_average"),

                Plot = string.IsNullOrWhiteSpace(plot)
                    ? "No description available."
                    : plot
            });
        }

        int currentPage = GetInt32(root, "page") ?? page;

        int totalPages =
            GetInt32(root, "total_pages") ?? currentPage;

        int? nextPage =
            currentPage < totalPages
                ? currentPage + 1
                : null;

        return (movies, nextPage);
    }

    private static string? GetString(
        JsonElement element,
        string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(
                propertyName, out JsonElement property) &&
            property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        return null;
    }

    private static int? GetInt32(
        JsonElement element,
        string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(
                propertyName, out JsonElement property) &&
            property.ValueKind == JsonValueKind.Number &&
            property.TryGetInt32(out int value))
        {
            return value;
        }

        return null;
    }

    private static double? GetDouble(
        JsonElement element,
        string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(
                propertyName, out JsonElement property) &&
            property.ValueKind == JsonValueKind.Number &&
            property.TryGetDouble(out double value))
        {
            return value;
        }

        return null;
    }

    private static int? GetReleaseYear(JsonElement movie)
    {
        string? releaseDate = GetString(movie, "release_date");

        if (DateTime.TryParseExact(
            releaseDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime date))
        {
            return date.Year;
        }

        return null;
    }

    private static string? GetPosterUrl(string? posterPath)
    {
        if (string.IsNullOrWhiteSpace(posterPath))
            return null;

        return $"{PosterBaseUrl}/{posterPath.TrimStart('/')}";
    }
}
