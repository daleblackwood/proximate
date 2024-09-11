
namespace Proximate;

public class StatusEndpoint : Endpoint
{
    public enum HealthStatus
    {
        INIT,
        OK,
        DEAD,
        STALE

    }

    public class RecorderHealth
    {
        public string Status { get; set; } = "INIT";
        public DateTime? First { get; set; }
        public DateTime? Last { get; set; }
        public DateTime? Next { get; set; }
        public DateTime? Polled { get; set; }
        public string Size { get; set; } = "0mb";
    }

    public class StatusResult
    {
        public DateTime Time { get; set; } = Timing.Now;
        public Dictionary<string, RecorderHealth> Recorders { get; set; } = new();
    }

    public override Task<object> Get(EndpointContext context)
    {
        var result = new StatusResult();
        var recorders = Services.Get<RecorderService>().GetAll();
        foreach (var recorder in recorders)
        {
            var lastUpdate = recorder.LastUpdateTime;
            var status = HealthStatus.OK;
            if (lastUpdate.Year == Timing.Epoch.Year)
            {
                status = HealthStatus.INIT;
            }
            else if (Timing.Since(lastUpdate).TotalDays > 30.0)
            {
                status = HealthStatus.DEAD;
            }
            else if (Timing.Since(lastUpdate).TotalDays > 1.0)
            {
                status = HealthStatus.STALE;
            }
            result.Recorders[recorder.Id] = new RecorderHealth {
                First = recorder.FirstUpdateTime,
                Last = recorder.LastUpdateTime,
                Next = recorder.Consumer.NextUpdateTime,
                Size = (recorder.MemorySize / 1024 / 1024).ToString("0.00") + "mb",
                Polled = recorder.Consumer.LastUpdateTime,
                Status = status.ToString()
            };
        }
        return Task.FromResult<object>(result);
    }
}