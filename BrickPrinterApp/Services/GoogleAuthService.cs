using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Util.Store;
using Newtonsoft.Json;

namespace BrickPrinterApp.Services;

/// <summary>
/// Handles Google OAuth 2.0 authentication for multiple Google services.
/// Supports Calendar, and can be extended for Gmail, Drive, etc.
/// </summary>
public class GoogleAuthService
{
    // Token storage in AppData (user-specific)
    private static readonly string TokenFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BrickPrinterApp", "google");

    // Client secrets bundled with app (in app directory)
    private static readonly string ClientSecretsFile = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "credentials.json");

    // FileDataStore saves tokens with this pattern
    private static readonly string TokenFilePattern = "Google.Apis.Auth.OAuth2.Responses.TokenResponse-user";

    private UserCredential? _credential;
    private GoogleAuthStatus _status = GoogleAuthStatus.NotConfigured;
    private string? _userEmail;

    /// <summary>
    /// Current authentication status
    /// </summary>
    public GoogleAuthStatus Status => _status;

    /// <summary>
    /// Email of the authenticated user (if logged in)
    /// </summary>
    public string? UserEmail => _userEmail;

    /// <summary>
    /// Returns true if credentials.json exists
    /// </summary>
    public bool HasClientSecrets => File.Exists(ClientSecretsFile);

    /// <summary>
    /// Path where the user should place their credentials.json file
    /// </summary>
    public string ClientSecretsPath => ClientSecretsFile;

    /// <summary>
    /// Currently requested OAuth scopes
    /// </summary>
    public static readonly string[] Scopes = new[]
    {
        CalendarService.Scope.CalendarReadonly,
        "email",
        "profile"
    };

    public GoogleAuthService()
    {
        Directory.CreateDirectory(TokenFolder);
        CheckInitialStatus();
    }

    private void CheckInitialStatus()
    {
        if (!HasClientSecrets)
        {
            _status = GoogleAuthStatus.NotConfigured;
            return;
        }

        if (HasSavedToken())
        {
            _status = GoogleAuthStatus.Unknown;
        }
        else
        {
            _status = GoogleAuthStatus.NotLoggedIn;
        }
    }

    private bool HasSavedToken()
    {
        var tokenFile = Path.Combine(TokenFolder, TokenFilePattern);
        return File.Exists(tokenFile);
    }

    /// <summary>
    /// Attempts to verify the current token and get user info.
    /// Call this on startup to check if the saved token is still valid.
    /// </summary>
    public async Task<bool> VerifyConnectionAsync()
    {
        if (!HasClientSecrets)
        {
            _status = GoogleAuthStatus.NotConfigured;
            return false;
        }

        if (!HasSavedToken())
        {
            _status = GoogleAuthStatus.NotLoggedIn;
            return false;
        }

        try
        {
            var credential = await LoadCredentialAsync();
            if (credential == null)
            {
                _status = GoogleAuthStatus.NotLoggedIn;
                return false;
            }

            // Try to refresh the token to verify it's still valid
            if (credential.Token.IsStale)
            {
                var success = await credential.RefreshTokenAsync(CancellationToken.None);
                if (!success)
                {
                    _status = GoogleAuthStatus.TokenExpired;
                    return false;
                }
            }

            // Get user info
            await FetchUserInfoAsync(credential);

            _credential = credential;
            _status = GoogleAuthStatus.Connected;
            return true;
        }
        catch (Exception)
        {
            _status = GoogleAuthStatus.Error;
            return false;
        }
    }

    /// <summary>
    /// Initiates the OAuth login flow. Opens browser for user to authenticate.
    /// </summary>
    public async Task<bool> LoginAsync()
    {
        if (!HasClientSecrets)
        {
            _status = GoogleAuthStatus.NotConfigured;
            return false;
        }

        try
        {
            _status = GoogleAuthStatus.Authenticating;

            using var stream = new FileStream(ClientSecretsFile, FileMode.Open, FileAccess.Read);
            var clientSecrets = GoogleClientSecrets.FromStream(stream);

            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets.Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(TokenFolder, true));

            // Get user info
            await FetchUserInfoAsync(_credential);

            _status = GoogleAuthStatus.Connected;
            return true;
        }
        catch (Exception)
        {
            _status = GoogleAuthStatus.Error;
            return false;
        }
    }

    /// <summary>
    /// Logs out by deleting the stored token
    /// </summary>
    public void Logout()
    {
        try
        {
            // Delete the Google.Apis.Auth token files
            var tokenFiles = Directory.GetFiles(TokenFolder, "Google.Apis.Auth.*");
            foreach (var file in tokenFiles)
            {
                File.Delete(file);
            }
        }
        catch
        {
            // Ignore deletion errors
        }

        _credential = null;
        _userEmail = null;
        _status = GoogleAuthStatus.NotLoggedIn;
    }

    /// <summary>
    /// Gets the current user credential for use with Google APIs.
    /// Returns null if not authenticated.
    /// </summary>
    public UserCredential? GetCredential()
    {
        return _credential;
    }

    /// <summary>
    /// Gets a fresh access token for API calls.
    /// Automatically refreshes if needed.
    /// </summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        if (_credential == null)
            return null;

        if (_credential.Token.IsStale)
        {
            await _credential.RefreshTokenAsync(CancellationToken.None);
        }

        return _credential.Token.AccessToken;
    }

    private async Task<UserCredential?> LoadCredentialAsync()
    {
        try
        {
            using var stream = new FileStream(ClientSecretsFile, FileMode.Open, FileAccess.Read);
            var clientSecrets = GoogleClientSecrets.FromStream(stream);

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets.Secrets,
                Scopes = Scopes,
                DataStore = new FileDataStore(TokenFolder, true)
            });

            var token = await flow.LoadTokenAsync("user", CancellationToken.None);
            if (token == null)
                return null;

            return new UserCredential(flow, "user", token);
        }
        catch
        {
            return null;
        }
    }

    private async Task FetchUserInfoAsync(UserCredential credential)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credential.Token.AccessToken);

            var response = await httpClient.GetStringAsync("https://www.googleapis.com/oauth2/v2/userinfo");
            var userInfo = JsonConvert.DeserializeObject<GoogleUserInfo>(response);
            _userEmail = userInfo?.Email;
        }
        catch
        {
            _userEmail = "Unknown";
        }
    }

    private class GoogleUserInfo
    {
        [JsonProperty("email")]
        public string? Email { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }
    }
}

public enum GoogleAuthStatus
{
    /// <summary>
    /// No credentials.json file found
    /// </summary>
    NotConfigured,

    /// <summary>
    /// Credentials exist but user hasn't logged in
    /// </summary>
    NotLoggedIn,

    /// <summary>
    /// Token exists but hasn't been verified yet
    /// </summary>
    Unknown,

    /// <summary>
    /// Currently performing OAuth flow
    /// </summary>
    Authenticating,

    /// <summary>
    /// Successfully connected and verified
    /// </summary>
    Connected,

    /// <summary>
    /// Token expired and couldn't be refreshed
    /// </summary>
    TokenExpired,

    /// <summary>
    /// An error occurred during authentication
    /// </summary>
    Error
}
