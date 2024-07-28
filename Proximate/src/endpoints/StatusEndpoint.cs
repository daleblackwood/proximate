
namespace Proximate;

public class HealthEndpoint : Endpoint
{
    public enum HealthStatus
    {
        EMPTY,
        OK,
        DEAD,
        STALE

    }

    public class HealthResult
    {
        public DateTime Time { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> Status { get; set; } = new();
        public Dictionary<string, DateTime> Updates { get; set; } = new();
    }

    public override Task<object> Get(Payload context)
    {
        var result = new HealthResult();
        var proxies = ProxyManager.Inst.GetAll();
        foreach (var proxy in proxies)
        {
            var lastUpdate = proxy.LastUpdateTime;
            result.Updates.Add(proxy.Id, proxy.LastUpdateTime);
            var status = HealthStatus.OK;
            if (lastUpdate.Year == DateTime.UnixEpoch.Year)
            {
                status = HealthStatus.EMPTY;
            }
            else if ((DateTime.UtcNow - lastUpdate).TotalDays > 30.0)
            {
                status = HealthStatus.DEAD;
            }
            else if ((DateTime.UtcNow - lastUpdate).TotalDays > 1.0)
            {
                status = HealthStatus.STALE;
            }
            result.Status.Add(proxy.Id, status.ToString());
        }
        return Task.FromResult<object>(result);
    }
}