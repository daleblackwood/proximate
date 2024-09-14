using EmbedIO;
using Proximate;

[AccessRequired]
public class LiveEndpoint : Endpoint
{
    public override async Task<object> Get(EndpointContext context)
    {
        var project = context.GetPath(1)?.ToLower();
        if (string.IsNullOrEmpty(project))
        {
            throw new HttpException(System.Net.HttpStatusCode.NotFound);
        }
        var config = new ApiConsumer.ApiConfig();
        config.LoadFile(Env.ProjectPath("config/" + project + ".config"));
        var consumer = ConsumerFactory.Instance.Build(config);
        var result = await consumer.Read();
        return result;
    }
}