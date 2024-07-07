using System.Net;

namespace Proximate;

public class RestProxy : IProxy
{
    private string url;
    private uint minPollIntervalMs = 200;
    private DateTime lastUpdateTime = DateTime.UnixEpoch;
    private string lastResult = string.Empty;
    private readonly HttpClient client = new();
    private IProxyAuthorizer? authorizer;


    public RestProxy(string url, uint minPollDurationMs = 200)
    {
        this.url = url;
        this.minPollIntervalMs = minPollDurationMs;
        client.Timeout = TimeSpan.FromSeconds(5);
    }

    public void SetHeader(string key, string value)
    {
        this.client.DefaultRequestHeaders.Add(key, value);
    }

    public void SetAuthorizer(IProxyAuthorizer authorizer)
    {
        this.authorizer = authorizer;
    }

    public async Task<string> Update()
    {
        var msSincePoll = (DateTime.UtcNow - lastUpdateTime).TotalMilliseconds;
        if (msSincePoll < minPollIntervalMs)
            return lastResult;

        var result = await Fetch();
        lastResult = result;
        lastUpdateTime = DateTime.UtcNow;
        return lastResult;
    }

    private async Task<string> Fetch()
    {
        try
        {
            var time = DateTime.UtcNow;
            Console.WriteLine($"Fetching data from {url}...");
            if (authorizer != null)
            {
                await authorizer.Authorize(client);
                if (authorizer.IsAuthorized == false)
                {
                    throw new Exception("Failed to authorize.");
                }
            }
            var response = await client.GetAsync(url);
            var duration = (DateTime.UtcNow - time).TotalMilliseconds;
            Console.WriteLine($"Fetched from {url} in {duration}ms.");
            var code = response.EnsureSuccessStatusCode()?.StatusCode;
            if (code != HttpStatusCode.OK)
            {
                throw new Exception($"Failed to fetch data: {code}");
            }
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching data: {ex.Message}");
            return string.Empty;
        }
    }

}