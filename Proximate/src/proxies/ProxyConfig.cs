namespace Proximate;

public class ProxyConfig
{
    public enum AuthProvider
    {
        None,
        OAuth
    }


    public readonly string? Url;
    public readonly string? OAuthUrl;
    public readonly string? ClientId;
    public readonly string? ClientSecret;
    
    private static Dictionary<string, ProxyConfig> cache = new();

    public static ProxyConfig Load(string keyOrPath)
    {
        var key = Path.GetFileNameWithoutExtension(keyOrPath.ToLower().Trim());
        if (cache.ContainsKey(key) == false)
        {
            Dictionary<string, string> values = new();
            var path = Path.IsPathRooted(keyOrPath) ? keyOrPath : Env.ProjectPath("config/" + key + ".config");
            if (File.Exists(path))
            {
                values = Env.LoadConfigFile(path);
            }
            else
            {
                var prefix = key.ToUpper() + "_";
                foreach (var envKey in Enum.GetNames<Env.Key>())
                {
                    if (envKey.StartsWith("API") == false)
                        continue;

                    var value = Env.Get(prefix + envKey, false);
                    if (string.IsNullOrEmpty(value) == false)
                    {
                        values[envKey] = value;
                    }
                }
            }
            cache[key] = new ProxyConfig(values);
        }
        return cache[key];
    }

    protected ProxyConfig(string url)
    {
        Url = url;
        Validate();
    }

    protected ProxyConfig(Dictionary<string, string> values) 
    {
        values.TryGetValue(Env.Key.API_URL.ToString(), out Url);
        values.TryGetValue(Env.Key.API_OAUTH_URL.ToString(), out OAuthUrl);
        values.TryGetValue(Env.Key.API_CLIENT_ID.ToString(), out ClientId);
        values.TryGetValue(Env.Key.API_CLIENT_SECRET.ToString(), out ClientSecret);
        Validate();
    }

    protected void Validate()
    {
        if (string.IsNullOrEmpty(Url) || Url.StartsWith("http") == false)
        {
            throw new Exception("Invalid API URL");
        }

        if (string.IsNullOrEmpty(OAuthUrl) == false && (
            string.IsNullOrEmpty(ClientId) || 
            string.IsNullOrEmpty(ClientSecret)
        ))
        {
            throw new Exception("Invalid OAuth Credentials");
        }
    }

    public IProxy CreateRestProxy()
    {
        var proxy = new RestProxy(Url!);
        if (OAuthUrl != null)
        {
            proxy.SetAuthorizer(new RestOAuthorizer(
                OAuthUrl,
                ClientId!,
                ClientSecret!
            ));        
        }
        return proxy;
    }
}