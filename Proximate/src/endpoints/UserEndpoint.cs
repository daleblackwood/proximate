using EmbedIO;
using Proximate;

[Access(AccessLevel.Admin)]
public class UserEndpoint : Endpoint
{

    public override async Task<object> Get(EndpointContext context)
    {
        try 
        {
            var user = await Services.Get<UserService>().GetUser(context.GetPath(0));
            if (user == null)
            {
                throw new Exception("No user.");
            }
            return user;
        }
        catch
        {
            throw new HttpException(System.Net.HttpStatusCode.NotFound);
        }
    }

    public class PostUserRequest 
    { 
        public string? email { get; set; }
        public string? password { get; set; }
        public string? access { get; set; }
    }

    public override async Task<object> Post(EndpointContext context)
    {
        var userService = Services.Get<UserService>();
        var request = context.GetData<PostUserRequest>();
        var accessLevel = AccessAttribute.ParseLevel(request?.access);
        await userService.AddUser(request?.email ?? "", request?.password ?? "", accessLevel);
        return true;
    }
}