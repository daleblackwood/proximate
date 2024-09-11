using Xunit;

namespace Proximate.Tests;

public class ConfigTest
{
    [Fact]
    public void ItShouldLoadConfig()
    {
        var config = new ApiConsumer.ApiConfig().LoadFile(Env.ProjectPath("data/openf1.config"));
        Assert.True(config != null);
        Assert.True(config.Get(ApiConsumer.ApiConfigKey.API_URL) != null);
        Assert.True(config.Get(ApiConsumer.ApiConfigKey.API_OAUTH_URL) != null);
        Assert.Equal("FAKE_USERNAME", config.Get(ApiConsumer.ApiConfigKey.API_CLIENT_ID));
        Assert.Equal("FAKE_PASSWORD", config.Get(ApiConsumer.ApiConfigKey.API_CLIENT_SECRET));
        Assert.Equal(TimeSpan.FromMinutes(10), Config.ParseSpan(config.Get(ApiConsumer.ApiConfigKey.API_POLL_MINS), TimeSpan.FromMinutes));
        Assert.Equal(60, Config.ParseInt(config.Get(ApiConsumer.ApiConfigKey.API_KEY_INTERVAL)));
    }
}
