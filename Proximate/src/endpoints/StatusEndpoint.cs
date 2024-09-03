
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
        public string Size { get; set; } = "0mb";
    }

    public class HealthResult
    {
        public DateTime Time { get; set; } = DateTime.UtcNow;
        public Dictionary<string, RecorderHealth> Recorders { get; set; } = new();
    }

    public override Task<object> Get(EndpointContext context)
    {
        var result = new HealthResult();
        var recorders = Services.Get<ProxyRecorderService>().GetAll();
        foreach (var recorder in recorders)
        {
            var lastUpdate = recorder.LastUpdateTime;
            var status = HealthStatus.OK;
            if (lastUpdate.Year == DateTime.UnixEpoch.Year)
            {
                status = HealthStatus.INIT;
            }
            else if ((DateTime.UtcNow - lastUpdate).TotalDays > 30.0)
            {
                status = HealthStatus.DEAD;
            }
            else if ((DateTime.UtcNow - lastUpdate).TotalDays > 1.0)
            {
                status = HealthStatus.STALE;
            }
            result.Recorders[recorder.Id] = new RecorderHealth {
                First = recorder.FirstSnapshot?.Time,
                Last = recorder.LastUpdateTime,
                Size = (recorder.StorageSize / 1024 / 1024).ToString("0.00") + "mb",
                Status = status.ToString()
            };
        }
        return Task.FromResult<object>(result);
    }
}