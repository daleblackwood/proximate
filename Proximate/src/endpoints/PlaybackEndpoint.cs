using EmbedIO;
using Proximate;

public class PlaybackEndpoint : Endpoint
{
    public override async Task<object> Get(Payload context)
    {
        var proxyUrl = Env.Get(Env.Key.API_URL, true);
        var proxy = ProxyManager.Inst.Get(RestProxy.ToId(proxyUrl));
        if (proxy == null)
        {
            var restProxy = new RestProxy(proxyUrl, 5000);
            var authorizer = new RestOAuthorizer(
                Env.Get(Env.Key.API_OAUTH_URL, true), 
                Env.Get(Env.Key.API_CLIENT_ID, true), 
                Env.Get(Env.Key.API_CLIENT_SECRET, true)
            );
            restProxy.SetAuthorizer(authorizer);
            proxy = ProxyManager.Inst.Register(restProxy);
        }

        var result = await proxy.Update();
        return result;
    }
}