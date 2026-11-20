using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Maui.Authentication;
using MauiStartup.Services;

namespace MauiStartup;

[Activity(
    NoHistory = true,
    LaunchMode = LaunchMode.SingleTop,
    Exported = true)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[]
    {
        Intent.CategoryDefault,
        Intent.CategoryBrowsable
    },
    DataScheme = GoogleAuthSettings.CallbackScheme)]
public class WebAuthenticationCallbackActivity
    : WebAuthenticatorCallbackActivity
{
}