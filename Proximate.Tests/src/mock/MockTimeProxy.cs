namespace Proximate.Tests;

public class MockTimeProxy : IProxy
{
    public const int BUILT_IN_DELAY_MS = 1;
    private DateTime? currentTime = null;

    public class Response
    {
        public DateTime time { get; set; }
    }

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
        var result = Json.Serialize(response);
        await Task.Delay(BUILT_IN_DELAY_MS);
        return result;
    }

}