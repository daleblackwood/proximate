using Proximate;

public class ProxyEndpoint : Endpoint
{
    private RestProxy? proxy;

    [Get("proxy")]
    public async Task<string> GetProxy()
    {
        if (proxy == null)
        {
            proxy = new RestProxy(Env.Get(Env.Key.API_URL, true));
            var authorizer = new RestOAuthorizer(
                Env.Get(Env.Key.API_OAUTH_URL, true), 
                Env.Get(Env.Key.API_CLIENT_ID, true), 
                Env.Get(Env.Key.API_CLIENT_SECRET, true)
            );
            proxy.SetAuthorizer(authorizer);
        }
        return await proxy.Update();
    }
}