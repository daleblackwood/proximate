
using EmbedIO.WebApi;

namespace Proximate;

[AccessRequired]
public class FeedsEndpoint : Endpoint
{
    public override async Task<object> Get(EndpointContext context)
    {
        var list = new List<string>();
        var services = Services.Get<RecorderService>().GetAll();
        foreach (var service in services) 
        {
            list.Add("feed/" + service.Id);
        }
        await Task.Delay(1);
        return list.ToArray();
    }
}