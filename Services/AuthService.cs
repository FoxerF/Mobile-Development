using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using MauiStartup.Models;
using Microsoft.Maui.Authentication;
using Microsoft.Maui.Storage;

namespace MauiStartup.Services;

public partial class AuthService : ObservableObject
{
    private const string AuthorizationEndpoint =
        "https://accounts.google.com/o/oauth2/v2/auth";

    private const string TokenEndpoint =
        "https://oauth2.googleapis.com/token";

    private const string UserInfoEndpoint =
        "https://openidconnect.googleapis.com/v1/userinfo";

    private const string RefreshTokenKey =
        "google_refresh_token";

    private const string AccessTokenKey =
        "google_access_token";

    private const string UserPreferenceKey =
        "google_auth_user";

    private const string ExpiryPreferenceKey =
        "google_access_expiry";

    private readonly HttpClient _httpClient;

    private readonly SemaphoreSlim _operationLock = new(1, 1);

    [ObservableProperty]
    private AuthUser? currentUser;

    [ObservableProperty]
    private string lastErrorMessage = string.Empty;

    public bool IsSignedIn => CurrentUser is not null;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    partial void OnCurrentUserChanged(AuthUser? value)
    {
        OnPropertyChanged(nameof(IsSignedIn));
    }

    public async Task<AuthUser> SignInAsync()
    {
        await _operationLock.WaitAsync();

        try
        {
            LastErrorMessage = string.Empty;

            string codeVerifier = GenerateRandomString();

            string codeChallenge =
                CreateCodeChallenge(codeVerifier);

            string state = GenerateRandomString();

            string authorizationUrl =
                BuildAuthorizationUrl(
                    codeChallenge,
                    state);

            WebAuthenticatorResult result =
                await WebAuthenticator.Default.AuthenticateAsync(
                    new Uri(authorizationUrl),
                    new Uri(GoogleAuthSettings.RedirectUri));

            string? returnedState = result.Get("state");

            if (!string.Equals(
                state,
                returnedState,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "OAuth state validation failed.");
            }

            string? authorizationCode = result.Get("code");

            if (string.IsNullOrWhiteSpace(authorizationCode))
            {
                string? oauthError = result.Get("error");

                throw new InvalidOperationException(
                    oauthError ??
                    "Google did not return an authorization code.");
            }

            TokenResponse tokens =
                await ExchangeAuthorizationCodeAsync(
                    authorizationCode,
                    codeVerifier);

            if (string.IsNullOrWhiteSpace(tokens.RefreshToken))
            {
                throw new InvalidOperationException(
                    "Google did not return a refresh token. " +
                    "Check offline access and consent settings, " +
                    "then try authorizing again.");
            }

            AuthUser user =
                await GetUserInfoAsync(tokens.AccessToken);

            await SecureStorage.Default.SetAsync(
                RefreshTokenKey,
                tokens.RefreshToken);

            await SecureStorage.Default.SetAsync(
                AccessTokenKey,
                tokens.AccessToken);

            Preferences.Default.Set(
                ExpiryPreferenceKey,
                DateTimeOffset.UtcNow
                    .AddSeconds(tokens.ExpiresIn)
                    .ToUnixTimeSeconds());

            Preferences.Default.Set(
                UserPreferenceKey,
                JsonSerializer.Serialize(user));

            CurrentUser = user;

            return user;
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<bool> RestoreSessionAsync()
    {
        await _operationLock.WaitAsync();

        try
        {
            if (CurrentUser is not null)
                return true;

            string? refreshToken =
                await SecureStorage.Default.GetAsync(
                    RefreshTokenKey);

            string? savedUser =
                Preferences.Default.Get(
                    UserPreferenceKey,
                    string.Empty);

            if (string.IsNullOrWhiteSpace(refreshToken) ||
                string.IsNullOrWhiteSpace(savedUser))
            {
                return false;
            }

            AuthUser? user =
                JsonSerializer.Deserialize<AuthUser>(savedUser);

            if (user is null ||
                string.IsNullOrWhiteSpace(user.Id))
            {
                await ClearSessionAsync();
                return false;
            }

            CurrentUser = user;

            bool refreshed =
                await RefreshAccessTokenCoreAsync(refreshToken);

            return CurrentUser is not null;
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;

            return CurrentUser is not null;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<bool> RefreshAccessTokenAsync()
    {
        await _operationLock.WaitAsync();

        try
        {
            string? refreshToken =
                await SecureStorage.Default.GetAsync(
                    RefreshTokenKey);

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                LastErrorMessage =
                    "No refresh token is available.";

                return false;
            }

            return await RefreshAccessTokenCoreAsync(refreshToken);
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            return false;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    private async Task<bool> RefreshAccessTokenCoreAsync(
        string refreshToken)
    {
        using var response =
            await _httpClient.PostAsync(
                TokenEndpoint,
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["client_id"] =
                            GoogleAuthSettings.ClientId,

                        ["grant_type"] =
                            "refresh_token",

                        ["refresh_token"] =
                            refreshToken
                    }));

        string json =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            string? error = ReadOAuthError(json);

            LastErrorMessage =
                $"Token refresh failed: {error ?? response.StatusCode.ToString()}";

            if (string.Equals(
                error,
                "invalid_grant",
                StringComparison.Ordinal))
            {
                await ClearSessionAsync();
            }

            return false;
        }

        using JsonDocument document =
            JsonDocument.Parse(json);

        JsonElement root = document.RootElement;

        string? accessToken =
            root.GetProperty("access_token").GetString();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            LastErrorMessage =
                "Google returned an empty access token.";

            return false;
        }

        int expiresIn =
            root.GetProperty("expires_in").GetInt32();

        await SecureStorage.Default.SetAsync(
            AccessTokenKey,
            accessToken);

        Preferences.Default.Set(
            ExpiryPreferenceKey,
            DateTimeOffset.UtcNow
                .AddSeconds(expiresIn)
                .ToUnixTimeSeconds());

        if (root.TryGetProperty(
            "refresh_token",
            out JsonElement newRefreshToken))
        {
            string? value = newRefreshToken.GetString();

            if (!string.IsNullOrWhiteSpace(value))
            {
                await SecureStorage.Default.SetAsync(
                    RefreshTokenKey,
                    value);
            }
        }

        LastErrorMessage = string.Empty;

        return true;
    }

    private async Task<TokenResponse>
        ExchangeAuthorizationCodeAsync(
            string authorizationCode,
            string codeVerifier)
    {
        var body = new Dictionary<string, string>
        {
            ["code"] = authorizationCode,

            ["client_id"] =
                GoogleAuthSettings.ClientId,

            ["redirect_uri"] =
                GoogleAuthSettings.RedirectUri,

            ["grant_type"] =
                "authorization_code",

            ["code_verifier"] =
                codeVerifier
        };

        using var response =
            await _httpClient.PostAsync(
                TokenEndpoint,
                new FormUrlEncodedContent(body));

        string json =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            string? error = ReadOAuthError(json);

            throw new InvalidOperationException(
                $"Google token exchange failed: " +
                $"{error ?? response.StatusCode.ToString()}. " +
                $"{json}");
        }

        using JsonDocument document =
            JsonDocument.Parse(json);

        JsonElement root = document.RootElement;

        string accessToken =
            root.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException(
                "Google returned no access token.");

        string? refreshToken = null;

        if (root.TryGetProperty(
            "refresh_token",
            out JsonElement refreshElement))
        {
            refreshToken = refreshElement.GetString();
        }

        int expiresIn =
            root.GetProperty("expires_in").GetInt32();

        return new TokenResponse(
            accessToken,
            refreshToken,
            expiresIn);
    }

    private async Task<AuthUser> GetUserInfoAsync(
        string accessToken)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                UserInfoEndpoint);

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        using var response =
            await _httpClient.SendAsync(request);

        string json =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Google profile request failed: {json}");
        }

        using JsonDocument document =
            JsonDocument.Parse(json);

        JsonElement root = document.RootElement;

        return new AuthUser
        {
            Id = root.GetProperty("sub").GetString()
                ?? string.Empty,

            Name = root.TryGetProperty(
                "name",
                out JsonElement name)
                ? name.GetString() ?? "Google user"
                : "Google user",

            Email = root.TryGetProperty(
                "email",
                out JsonElement email)
                ? email.GetString() ?? string.Empty
                : string.Empty,

            PictureUrl = root.TryGetProperty(
                "picture",
                out JsonElement picture)
                ? picture.GetString()
                : null
        };
    }

    private string BuildAuthorizationUrl(
        string codeChallenge,
        string state)
    {
        var parameters =
            new Dictionary<string, string>
            {
                ["response_type"] = "code",

                ["client_id"] =
                    GoogleAuthSettings.ClientId,

                ["redirect_uri"] =
                    GoogleAuthSettings.RedirectUri,

                ["scope"] =
                    "openid profile email",

                ["code_challenge"] =
                    codeChallenge,

                ["code_challenge_method"] =
                    "S256",

                ["state"] =
                    state,

                ["access_type"] =
                    "offline",

                ["prompt"] =
                    "consent"
            };

        var query = string.Join(
            "&",
            parameters.Select(
                pair =>
                    $"{Uri.EscapeDataString(pair.Key)}=" +
                    $"{Uri.EscapeDataString(pair.Value)}"));

        return $"{AuthorizationEndpoint}?{query}";
    }

    private static string GenerateRandomString()
    {
        byte[] bytes =
            RandomNumberGenerator.GetBytes(32);

        return Base64UrlEncode(bytes);
    }

    private static string CreateCodeChallenge(
        string verifier)
    {
        byte[] hash =
            SHA256.HashData(
                Encoding.ASCII.GetBytes(verifier));

        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string? ReadOAuthError(string json)
    {
        try
        {
            using JsonDocument document =
                JsonDocument.Parse(json);

            if (document.RootElement.TryGetProperty(
                "error",
                out JsonElement error))
            {
                return error.GetString();
            }
        }
        catch
        {
        }

        return null;
    }

    public async Task SignOutAsync()
    {
        await _operationLock.WaitAsync();

        try
        {
            await ClearSessionAsync();
        }
        finally
        {
            _operationLock.Release();
        }
    }

    private async Task ClearSessionAsync()
    {
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(AccessTokenKey);

        Preferences.Default.Remove(UserPreferenceKey);
        Preferences.Default.Remove(ExpiryPreferenceKey);

        CurrentUser = null;
    }

    private sealed record TokenResponse(
        string AccessToken,
        string? RefreshToken,
        int ExpiresIn);
}
