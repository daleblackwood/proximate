
using EmbedIO.WebApi;

namespace Proximate;

[AccessOpen]
public class RootEndpoint : Endpoint
{
    public override async Task<object> Get(EndpointContext context)
    {
        await Task.Delay(1);
        return "OK";
    }
}