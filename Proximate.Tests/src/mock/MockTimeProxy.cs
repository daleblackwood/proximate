namespace Proximate.Tests;

public class MockTimeProxy : IConsumer
{
    public const int BUILT_IN_DELAY_MS = 1;

    public const string ID = "mock-time-proxy";
    public string Id => ID;


    public class Response
    {
        public required DateTime time { get; set; }
        public required int year { get; set; }
        public required int month { get; set; }
        public required int day { get; set; }
        public required int hour { get; set; }
        public required int minute { get; set; }
        public required int second { get; set; }

        public required string formatted { get; set; }
    }

    public DateTime LastUpdateTime { get; private set; } = Timing.Epoch;
    public DateTime NextUpdateTime => LastUpdateTime.AddSeconds(1);

    private DateTime? currentTime = null;


    public void SetTime(DateTime? time)
    {
        currentTime = time;
    }

    public async Task<ConsumerMessage[]> Read()
    {
        var time = (DateTime)(currentTime != null ? currentTime : Timing.Now);
        var response = new Response { 
            time = time,
            year = time.Year,
            month = time.Month,
            day = time.Day,
            hour = time.Hour,
            minute = time.Minute,
            second = time.Second,
            formatted = time.ToString()
        };
        var content = Json.Serialize(response);
        var message = new ConsumerMessage { 
            time = time,
            content = content 
        };
        await Task.Delay(BUILT_IN_DELAY_MS);
        LastUpdateTime = response.time.AddMilliseconds(BUILT_IN_DELAY_MS);
        return [message];
    }

}