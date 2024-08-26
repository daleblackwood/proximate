using EmbedIO;
using Proximate;

public class LiveEndpoint : Endpoint
{
    public override async Task<object> Get(EndpointContext context)
    {
        var project = context.GetPath(1)?.ToLower();
        if (string.IsNullOrEmpty(project))
        {
            throw new HttpException(System.Net.HttpStatusCode.NotFound);
        }

        var config = ProxyConfig.Load(project);
        var proxy = config.CreateRestProxy();
        var result = await proxy.Update();
        return result;
    }
}