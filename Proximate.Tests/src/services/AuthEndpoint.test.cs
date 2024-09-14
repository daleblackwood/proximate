using Xunit;

namespace Proximate.Tests;

public class AuthEndpointTest
{    
    private async Task CreateTestUser()
    {
        var users = Services.Get<UserService>();
        if ((await users.HasUser("test@test.com")) == false)
        {
            await users.AddUser("test@test.com", "testing123");
        }
    }

    [Fact]
    public async Task AuthServiceLogin()
    {
        await CreateTestUser();
        var token = await Services.Get<AuthService>().Login("test@test.com", "testing123");
        Assert.True(token != null, "It should get a token");
    }

    [Fact]
    public async Task AuthServiceAccess()
    {
        await CreateTestUser();
        var authService = Services.Get<AuthService>();
        var token = await authService.Login("test@test.com", "testing123");
        var canAccess = await authService.CanAccess(token, AccessLevel.Required);
        Assert.True(canAccess, "Should be able to access.");
    }
}
