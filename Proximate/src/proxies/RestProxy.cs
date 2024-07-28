using System.Net;

namespace Proximate;

public class RestProxy : IProxy
{
    public static string ToId(string url) => "rest-" + url;

    public string Id => ToId(url);

    public string LastResult { get; private set; } = string.Empty;
    public DateTime LastUpdateTime { get; private set; } = DateTime.UnixEpoch;

    private string url;
    private uint minPollIntervalMs = 200;
    private readonly HttpClient client = new();
    private IProxyAuthorizer? authorizer;


    public RestProxy(string url, uint minPollDurationMs = 200)
    {
        this.url = url;
        this.minPollIntervalMs = minPollDurationMs;
        client.Timeout = TimeSpan.FromSeconds(5);
        ProxyManager.Inst.Register(this);
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
        var msSincePoll = (DateTime.UtcNow - LastUpdateTime).TotalMilliseconds;
        if (msSincePoll < minPollIntervalMs)
            return LastResult;

        var result = await Fetch();
        LastResult = result;
        LastUpdateTime = DateTime.UtcNow;
        return LastResult;
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