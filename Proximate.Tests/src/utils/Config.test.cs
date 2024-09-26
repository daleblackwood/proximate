using Xunit;

namespace Proximate.Tests;

public class ConfigTest
{
    [Fact]
    public void LoadConfig()
    {
        var config = new ApiConsumer.ApiConfig().LoadFile(Env.ProjectPath("data/openf1.toml"));
        Assert.True(config != null);
        Assert.True(config.Get(ApiConsumer.ApiConfigKey.API_URL) != null);
        Assert.True(config.Get(ApiConsumer.ApiConfigKey.API_OAUTH_URL) != null);
        Assert.Equal("fake-username", config.Get(ApiConsumer.ApiConfigKey.API_CLIENT_ID));
        Assert.Equal("fake-password", config.Get(ApiConsumer.ApiConfigKey.API_CLIENT_SECRET));
        Assert.Equal("password", config.Get(ApiConsumer.ApiConfigKey.API_OAUTH_GRANT));
        Assert.Equal(TimeSpan.FromMinutes(10), Config.ParseSpan(config.Get(ApiConsumer.ApiConfigKey.POLL_INTERVAL)));
        Assert.Equal(120, Config.ParseSpan(config.Get(ApiConsumer.ApiConfigKey.KEY_INTERVAL)).TotalMinutes);

        config = new ApiConsumer.ApiConfig().LoadFile(Env.ProjectPath("data/time.toml"));
        Assert.Equal(TimeSpan.FromMinutes(1), Config.ParseSpan(config.Get(ApiConsumer.ApiConfigKey.POLL_INTERVAL)));
        Assert.Equal(30, Config.ParseSpan(config.Get(ApiConsumer.ApiConfigKey.KEY_INTERVAL), TimeSpan.FromMinutes(30)).TotalMinutes);
    }

    [Fact]
    public void ParseConfigSpans()
    {
        Assert.Equal(2.0, Config.ParseSpan("0:00:02.00").TotalSeconds);
        Assert.Equal(2.0, Config.ParseSpan("0:02:00.00").TotalMinutes);
        Assert.Equal(2.5, Config.ParseSpan("0:02:30.00").TotalMinutes);
        Assert.Equal(2.0, Config.ParseSpan("2:00:00.00").TotalHours);
        Assert.Equal(500.0, Config.ParseSpan("0:00:00.5").TotalMilliseconds);
        Assert.Equal((60 * 60) + 60 + 10 + 0.5, Config.ParseSpan("1:01:10.5").TotalSeconds);
    }
}
