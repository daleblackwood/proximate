using System.Text;
using Xunit;

namespace Proximate.Tests;

public class OAuthTest
{
    private const string API_CONFIG = "QVBJX1VSTD0iaHR0cHM6Ly9kZW1vLmR1ZW5kZXNvZnR3YXJlLmNvbS9hcGkvdGVzdCIKQVBJX09BVVRIX1VSTD0iaHR0cHM6Ly9kZW1vLmR1ZW5kZXNvZnR3YXJlLmNvbS9jb25uZWN0L3Rva2VuIgpBUElfQ0xJRU5UX0lEPSJtMm0iCkFQSV9DTElFTlRfU0VDUkVUPSJzZWNyZXQi";

    private static void LoadDemo()
    {
        Env.LoadText(Encoding.UTF8.GetString(Convert.FromBase64String(API_CONFIG)));
    }

    [Fact]
    public async Task ShouldPassOAuth()
    {
        LoadDemo();

        var proxy = new RestProxy(Env.Get(Env.Key.API_URL, true));
        var authorizer = new RestOAuthorizer(
            Env.Get(Env.Key.API_OAUTH_URL, true), 
            Env.Get(Env.Key.API_CLIENT_ID, true), 
            Env.Get(Env.Key.API_CLIENT_SECRET, true)
        );
        proxy.SetAuthorizer(authorizer);        
        
        var response = await proxy.Update();

        Assert.True(authorizer.IsAuthorized, "Authorizer should be authorized");
        Assert.True(string.IsNullOrEmpty(response) == false, "Response should be present");
    }
}