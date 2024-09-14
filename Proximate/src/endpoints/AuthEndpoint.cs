using EmbedIO;
using Proximate;

[AccessOpen]
public class AuthEndpoint : Endpoint
{
    public override async Task<object> Get(EndpointContext context)
    {
        string? token = null;
        try 
        {
            token = await Services.Get<AuthService>().Login(context.RequestHeaders);
        } 
        catch 
        {
            throw new HttpException(System.Net.HttpStatusCode.Forbidden);
        }
        return token;
    }
}