namespace Proximate.Tests;

public class MockTimeProxy : IProxy
{
    public const int BUILT_IN_DELAY_MS = 1;

    public const string ID = "mock-time-proxy";
    public string Id => ID;


    public class Response
    {
        public DateTime time { get; set; }
    }

    public string LastResult { get; private set; } = string.Empty;
    public DateTime LastUpdateTime { get; private set; } = DateTime.UnixEpoch;

    private DateTime? currentTime = null;


    public void SetTime(DateTime? time)
    {
        currentTime = time;
    }

    public async Task<string> Update()
    {
        var response = new Response 
        { 
            time = (DateTime)(currentTime != null ? currentTime : DateTime.UtcNow)
        };
        LastResult = Json.Serialize(response);
        await Task.Delay(BUILT_IN_DELAY_MS);
        LastUpdateTime = response.time.AddMilliseconds(BUILT_IN_DELAY_MS);
        return LastResult;
    }

}