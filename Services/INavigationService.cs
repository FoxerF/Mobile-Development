
using MauiStartup.Models;
using System.Threading.Tasks;

namespace MauiStartup.Services;

public interface INavigationService
{
    Task ShowMovieDetailsAsync(Movie movie);
}
