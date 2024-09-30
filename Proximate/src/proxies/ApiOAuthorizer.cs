using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Proximate;

public class ApiOAuthorizer : IAuthorizer
{
    private class OAuthTokenResponse
    {
        public string? access_token { get; set; }
        public int expires_in { get; set; }
    }

    public bool IsAuthorized => accessToken != null || HasCredentials == false;

    private string tokenUrl;
    private string clientId;
    private string clientSecret;
    private string? audience;
    private string grantType;

    private DateTime nextRefreshTime = Timing.Epoch;
    private string? accessToken;

    private static readonly JsonSerializerOptions JsonOptions = new() { NumberHandling = JsonNumberHandling.AllowReadingFromString };

    private bool HasCredentials => string.IsNullOrEmpty(clientId) == false && string.IsNullOrEmpty(clientSecret) == false;

    // Expands ${VAR} references so credentials can live in the environment instead of config files.
    private static string ExpandEnv(string value) =>
        Regex.Replace(value, @"\$\{(\w+)\}", m => Environment.GetEnvironmentVariable(m.Groups[1].Value) ?? "");

    public ApiOAuthorizer(string tokenUrl, string clientId, string clientSecret, string? audience = null, string? grantType = null)
    {
        this.tokenUrl = tokenUrl;
        this.clientId = ExpandEnv(clientId);
        this.clientSecret = ExpandEnv(clientSecret);
        this.audience = audience;
        this.grantType = string.IsNullOrWhiteSpace(grantType) ? "client_credentials" : grantType;
    }

    public async Task Authorize(HttpClient client)
    {
        if (HasCredentials && Timing.Now >= nextRefreshTime)
        {
            var nextToken = await GetAccessToken();
            if (string.IsNullOrEmpty(nextToken) == false)
            {
                accessToken = nextToken;
            }
        }
        if (client.DefaultRequestHeaders.Contains("Authorization"))
        {
            client.DefaultRequestHeaders.Remove("Authorization");
        }
        if (accessToken != null)
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
        }
    }

    private async Task<string?> GetAccessToken()
    {
        using (var client = new HttpClient())
        {
            try
            {
                Log.Debug($"Getting access token from {tokenUrl}...");
                var formData = grantType == "password"
                    ? new Dictionary<string, string>
                    {
                        { "grant_type", "password" },
                        { "username", clientId },
                        { "password", clientSecret }
                    }
                    : new Dictionary<string, string>
                    {
                        { "grant_type", grantType },
                        { "client_id", clientId },
                        { "client_secret", clientSecret }
                    };

                if (audience != null)
                {
                    formData.Add("audience", audience);
                }

                var requestContent = new FormUrlEncodedContent(formData);

                var response = await client.PostAsync(tokenUrl, requestContent);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                if (responseContent == null)
                {
                    throw new Exception("empty response");
                }

                var tokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(responseContent, JsonOptions);
                if (string.IsNullOrEmpty(tokenResponse?.access_token))
                {
                   throw new Exception("no access token");
                }

                if (tokenResponse!.expires_in <= 0)
                {
                    tokenResponse.expires_in = 3600;
                }
                nextRefreshTime = Timing.Now.AddSeconds(tokenResponse!.expires_in / 2);

                return tokenResponse!.access_token;
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting access token: {ex.Message}");
                nextRefreshTime = Timing.Now;
                return null;
            }
        }
    }
}
