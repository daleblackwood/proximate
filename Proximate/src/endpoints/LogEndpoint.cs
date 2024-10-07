using EmbedIO;

namespace Proximate;

[AccessRequired]
public class LogEndpoint : Endpoint
{
    public override async Task<object> Get(EndpointContext context)
    {
        var logFile = context.GetPath(1)?.ToLower();
        if (string.IsNullOrEmpty(logFile))
        {
            throw new HttpException(System.Net.HttpStatusCode.NotFound);
        }

        var logPath = Path.Combine(Services.Get<SnapshotLogService>().GetLogDirectory(), logFile);
        if (File.Exists(logPath) == false)
        {
            throw new HttpException(System.Net.HttpStatusCode.NotFound);
        }
        var dateStr = context.GetParameter("date");
        if (DateTime.TryParse(dateStr, out var date) == false)
        {
            throw new HttpException(System.Net.HttpStatusCode.BadRequest, "Invalid date");
        }

        var (line, position) = LogReadUtils.ReadLine(logPath, date, true);

        await Task.Delay(1);

        if (string.IsNullOrEmpty(line) == false)
        {
            var lines = new List<string>();
            var deserialized = Snapshotter.Snapshot.Deserialize(line);
            do
            {
                lines.Add(deserialized.Value);
                var (newLine, newPosition) = LogReadUtils.ReadNextLine(logPath, position);
                if (String.IsNullOrEmpty(newLine))
                    break;
                deserialized = Snapshotter.Snapshot.Deserialize(line);
                line = newLine;
                position = newPosition;
            }
            while (deserialized.IsKey == false);
            line = string.Join('\n', lines.ToArray());
        }

        return line ?? "";
    }
}