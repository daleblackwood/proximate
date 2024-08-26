using System.Text.Json;

namespace Proximate;

public class RestOAuthorizer : IProxyAuthorizer
{
    // Helper class to deserialize the OAuth response
    private class OAuthTokenResponse
    {
        public string? access_token { get; set; }
        public int expires_in { get; set; }
    }

    public bool IsAuthorized => accessToken != null;

    private string tokenUrl;
    private string clientId;
    private string clientSecret;
    private string? audience;

    private DateTime nextRefreshTime = DateTime.UnixEpoch;
    private string? accessToken;

    public RestOAuthorizer(string tokenUrl, string clientId, string clientSecret, string? audience = null)
    {
        this.tokenUrl = tokenUrl;
        this.clientId = clientId;
        this.clientSecret = clientSecret;
        this.audience = audience;
    }

    public async Task Authorize(HttpClient client)
    {
        if (DateTime.UtcNow >= nextRefreshTime)
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
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
    }

    private async Task<string?> GetAccessToken()
    {
        using (var client = new HttpClient())
        {
            try
            {
                Log.Info($"Getting access token from {tokenUrl}...");
                var formData = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
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

                var tokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(responseContent);
                if (string.IsNullOrEmpty(tokenResponse?.access_token))
                {
                   throw new Exception("no access token");
                }

                if (tokenResponse!.expires_in <= 0)
                {
                    tokenResponse.expires_in = 3600;
                }
                nextRefreshTime = DateTime.UtcNow.AddSeconds(tokenResponse!.expires_in / 2);

                return tokenResponse!.access_token;
            }
            catch (Exception ex)
            {
                Log.Info($"Error getting access token: {ex.Message}");
                nextRefreshTime = DateTime.UtcNow;
                return null;
            }
        }
    }
}
