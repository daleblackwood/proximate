using System.Dynamic;
using System.Net;

namespace Proximate;

public class ApiConsumer : IConsumer
{
    public enum ApiConfigKey
    {
        API_URL,
        API_OAUTH_URL,
        API_OAUTH_GRANT,
        API_CLIENT_ID,
        API_CLIENT_SECRET,
        POLL_INTERVAL,
        KEY_INTERVAL
    }

    public class ApiConfig : Config<ApiConfigKey> 
    {
        public override void Validate()
        {
            var apiUrl = Get(ApiConfigKey.API_URL);
            if ((apiUrl?.StartsWith("http") ?? false) == false)
            {
                throw new Exception("Invalid API URL");
            }

            if (string.IsNullOrEmpty(Get(ApiConfigKey.API_OAUTH_URL)) == false && (
                string.IsNullOrEmpty(Get(ApiConfigKey.API_CLIENT_ID)) || 
                string.IsNullOrEmpty(Get(ApiConfigKey.API_CLIENT_SECRET))
            ))
            {
                throw new Exception("Invalid OAuth Credentials");
            }
        }
    }

    public class ApiConsumerBuilder : IConsumerBuilder
    {
        public bool CanBuild(Config config)
        {
            return config.Has(ApiConfigKey.API_URL);
        }

        public IConsumer Build(Config inConfig)
        {
            var config = new ApiConfig();
            config.Copy(inConfig);
            config.Id = inConfig.Id;
            config.Validate();

            var url = config.Get(ApiConfigKey.API_URL);
            var consumer = new ApiConsumer(config.Id!, url!);

            var oAuthUrl = config.Get(ApiConfigKey.API_OAUTH_URL);
            if (oAuthUrl != null)
            {
                consumer.SetAuthorizer(new ApiOAuthorizer(
                    oAuthUrl,
                    config.Get(ApiConfigKey.API_CLIENT_ID)!,
                    config.Get(ApiConfigKey.API_CLIENT_SECRET)!,
                    grantType: config.Get(ApiConfigKey.API_OAUTH_GRANT)
                ));        
            }
            return consumer;
        }
    }

    public string Id { get; private set; }

    public DateTime LastUpdateTime { get; private set; } = Timing.Epoch;
    public DateTime NextUpdateTime => LastUpdateTime.AddMilliseconds(minPollIntervalMs);

    private string url;
    private uint minPollIntervalMs = 200;
    private readonly HttpClient client = new();
    private IAuthorizer? authorizer;


    public ApiConsumer(string id, string url, uint minPollDurationMs = 200)
    {
        this.Id = id;
        this.url = url;
        this.minPollIntervalMs = minPollDurationMs;
        client.Timeout = TimeSpan.FromSeconds(5);
    }

    public void SetHeader(string key, string value)
    {
        this.client.DefaultRequestHeaders.Add(key, value);
    }

    public void SetAuthorizer(IAuthorizer authorizer)
    {
        this.authorizer = authorizer;
    }

    public async Task<ConsumerMessage[]> Read()
    {
        var msSincePoll = Timing.Since(LastUpdateTime).TotalMilliseconds;
        if (msSincePoll < minPollIntervalMs)
            return Array.Empty<ConsumerMessage>();

        var message = await Fetch();
        var result = new ConsumerMessage {
            time = Timing.Now,
            content = message
        };
        LastUpdateTime = result.time;
        return new ConsumerMessage[] { result };
    }

    private async Task<string> Fetch()
    {
        try
        {
            var time = Timing.Now;
            Log.Info($"Fetching data from {url}...");
            if (authorizer != null)
            {
                await authorizer.Authorize(client);
                if (authorizer.IsAuthorized == false)
                {
                    throw new Exception("Failed to authorize.");
                }
            }
            var response = await client.GetAsync(url);
            var duration = Timing.Since(time).TotalMilliseconds;
            Log.Verbose($"Fetched from {url} in {duration}ms.");
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
            Log.Error($"Error fetching data: {ex.Message}");
            return string.Empty;
        }
    }

}