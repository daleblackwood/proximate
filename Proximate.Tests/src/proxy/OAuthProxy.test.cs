using Xunit;

namespace Proximate.Tests;

public sealed class OpenF1FactAttribute : FactAttribute
{
    public OpenF1FactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENF1_USERNAME")) ||
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENF1_PASSWORD")))
        {
            Skip = "Set OPENF1_USERNAME and OPENF1_PASSWORD to run against OpenF1";
        }
    }
}

public class OAuthTest
{
    private static async Task AssertAuthorizedRead(string id, string url, ApiOAuthorizer authorizer)
    {
        var proxy = new ApiConsumer(id, url);
        proxy.SetAuthorizer(authorizer);

        var responses = await proxy.Read();
        Assert.True(authorizer.IsAuthorized, "Authorizer should be authorized");
        Assert.True(responses.Length > 0, "Response should be present");
        Assert.True(string.IsNullOrEmpty(responses[0].content) == false, "Response should have value");
    }

    [OpenF1Fact]
    public async Task PassOAuth()
    {
        var authorizer = new ApiOAuthorizer(
            "https://api.openf1.org/token",
            "${OPENF1_USERNAME}",
            "${OPENF1_PASSWORD}",
            grantType: "password"
        );
        await AssertAuthorizedRead("test-openf1", "https://api.openf1.org/v1/sessions?session_key=latest", authorizer);
    }

    [Fact]
    public async Task PassOAuthClientCredentials()
    {
        var authorizer = new ApiOAuthorizer(
            "https://demo.duendesoftware.com/connect/token",
            "m2m",
            "secret"
        );
        await AssertAuthorizedRead("test-oauth-demo", "https://demo.duendesoftware.com/api/test", authorizer);
    }
}