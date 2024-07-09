using Xunit;

namespace Proximate.Tests;

public class MockTimeProxyTest
{

    [Fact]
    public async Task ProgressTime()
    {
        var proxy = new MockTimeProxy();
        var start = DateTime.UtcNow;
        var snapStr = await proxy.Update();
        var snap = Json.TryParse<MockTimeProxy.Response>(snapStr);
        Assert.True(snap != null, "Should have valid state 1");
        var diffMs = (snap!.time - start).TotalMilliseconds;
        Assert.True(diffMs < 100, "Time should be about now");

        await Task.Delay(100 - MockTimeProxy.BUILT_IN_DELAY_MS);
        snapStr = await proxy.Update();
        snap = Json.TryParse<MockTimeProxy.Response>(snapStr);
        Assert.True(snap != null, "Should have valid state 1 ");
        diffMs = (snap!.time - start).TotalMilliseconds;
        Assert.True(diffMs >= 100, "Time should be progressed by 100ms");
        Assert.True(diffMs < 150, "Time should be progressed by less than 110ms");

        await Task.Delay(100 - MockTimeProxy.BUILT_IN_DELAY_MS);
        snapStr = await proxy.Update();
        snap = Json.TryParse<MockTimeProxy.Response>(snapStr);
        Assert.True(snap != null, "Should have valid state 2");
        diffMs = (snap!.time - start).TotalMilliseconds;
        Assert.True(diffMs >= 200, "Time should be progressed by 200ms");
        Assert.True(diffMs < 250, "Time should be progressed by less than 210ms");
    }
}