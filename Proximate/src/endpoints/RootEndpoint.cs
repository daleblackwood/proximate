
using EmbedIO.WebApi;

namespace Proximate;

public class RootEndpoint : Endpoint
{
    public override async Task<object> Get(Payload context)
    {
        return "OK";
    }
}