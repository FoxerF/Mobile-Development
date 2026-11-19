using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MauiStartup.Models;
using Microsoft.Maui.Storage;

namespace MauiStartup.Services;

public class StorageService
{
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private string AppDirectory =>
        Path.Combine(
            FileSystem.AppDataDirectory,
            "MovieCatalog");

    private string PostersDirectory =>
        Path.Combine(
            AppDirectory,
            "Posters");

    private string MoviesFilePath =>
        Path.Combine(
            AppDirectory,
            "Movies.json");

    public async Task<List<Movie>> LoadMoviesAsync()
    {
        await _fileLock.WaitAsync();

        try
        {
            Directory.CreateDirectory(PostersDirectory);

            if (!File.Exists(MoviesFilePath))
                return new List<Movie>();

            string json =
                await File.ReadAllTextAsync(MoviesFilePath);

            return JsonSerializer.Deserialize<List<Movie>>(
                json,
                _jsonOptions) ?? new List<Movie>();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveMoviesAsync(
        IEnumerable<Movie> movies)
    {
        string temporaryFilePath =
            MoviesFilePath + ".tmp";

        await _fileLock.WaitAsync();

        try
        {
            Directory.CreateDirectory(PostersDirectory);

            List<Movie> userMovies =
                movies
                    .Where(movie => movie.IsUserCreated)
                    .ToList();

            string json =
                JsonSerializer.Serialize(
                    userMovies,
                    _jsonOptions);

            await File.WriteAllTextAsync(
                temporaryFilePath,
                json);

            File.Move(
                temporaryFilePath,
                MoviesFilePath,
                overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryFilePath))
                    File.Delete(temporaryFilePath);
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }

    public async Task<string> CopyPosterAsync(
        FileResult sourceFile)
    {
        if (sourceFile is null)
            throw new ArgumentNullException(nameof(sourceFile));

        Directory.CreateDirectory(PostersDirectory);

        string extension =
            Path.GetExtension(sourceFile.FileName);

        string[] allowedExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".gif",
            ".bmp",
            ".heic",
            ".heif"
        };

        if (!allowedExtensions.Contains(
            extension,
            StringComparer.OrdinalIgnoreCase))
        {
            extension = ".jpg";
        }

        string destinationPath =
            Path.Combine(
                PostersDirectory,
                $"{Guid.NewGuid():N}{extension}");

        try
        {
            await using Stream source =
                await sourceFile.OpenReadAsync();

            await using FileStream destination =
                new(
                    destinationPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);

            await source.CopyToAsync(destination);

            return destinationPath;
        }
        catch
        {
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            throw;
        }
    }

    public Task DeletePosterAsync(string? posterPath)
    {
        if (string.IsNullOrWhiteSpace(posterPath) ||
            !Path.IsPathRooted(posterPath))
        {
            return Task.CompletedTask;
        }

        string root =
            Path.GetFullPath(PostersDirectory);

        string target =
            Path.GetFullPath(posterPath);

        string relative =
            Path.GetRelativePath(root, target);

        bool outsideDirectory =
            relative == "." ||
            relative == ".." ||
            relative.StartsWith(
                ".." + Path.DirectorySeparatorChar,
                StringComparison.Ordinal) ||
            Path.IsPathRooted(relative);

        if (outsideDirectory)
            return Task.CompletedTask;

        if (File.Exists(target))
            File.Delete(target);

        return Task.CompletedTask;
    }
}