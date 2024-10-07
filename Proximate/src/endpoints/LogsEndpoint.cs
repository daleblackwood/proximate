namespace Proximate;

[AccessRequired]
public class LogsEndpoint : Endpoint
{
    public override async Task<object> Get(EndpointContext context)
    {
        var list = new List<string>();
        var logDir = Services.Get<SnapshotLogService>().GetLogDirectory();
        var logFiles = Directory.GetFiles(logDir);
        foreach (var file in logFiles) 
        {
            var key = file.Substring(logDir.Length + 1);
            list.Add(key);
        }
        await Task.Delay(1);
        return list.ToArray();
    }
}