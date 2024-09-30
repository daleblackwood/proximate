using EmbedIO;
using Proximate;

[AccessRequired]
public class PlaybackEndpoint : Endpoint
{
    public override async Task<object> Get(EndpointContext context)
    {
        var project = context.GetPath(1)?.ToLower();
        if (string.IsNullOrEmpty(project))
        {
            throw new HttpException(System.Net.HttpStatusCode.NotFound);
        }

        ConsumerRecorder recorder;
        try 
        {
            recorder = Services.Get<RecorderService>().Get(project);
        } 
        catch (Exception e)
        {
            throw new HttpException(System.Net.HttpStatusCode.NotFound, e.Message);
        }   

        var delay = context.GetParameterNumber("delay");
        var snapshot = recorder.Read((uint)delay * 1000u);

        if (snapshot != null)
        {
            context.SetHeader("Head", DateUtils.Format(snapshot.Time.AddSeconds(-delay)));
        }

        var logTime = Services.Get<SnapshotLogService>().GetLogTime(project);

        context.SetHeader("First", DateUtils.Format(recorder.FirstUpdateTime));
        context.SetHeader("Last", DateUtils.Format(recorder.LastSnapshotTime));
        context.SetHeader("Polled", DateUtils.Format(recorder.LastUpdateTime));
        context.SetHeader("Next", DateUtils.Format(recorder.NextUpdateTime));
        context.SetHeader("In-Memory", $"{Math.Ceiling((double)(recorder.MemorySize / 1024) / 1024 * 100) / 100.0}mb");
        context.SetHeader("Keys", recorder.KeyshotCount.ToString());
        context.SetHeader("Log", DateUtils.Format(logTime));
        context.SetHeader("Snapshots", recorder.SnapshotCount.ToString());

        var attemptsLeft = 10;
        for (int attempt = 0; attempt < attemptsLeft; attempt++)
        {
            if (recorder.LastSnapshot != null)
                break;

            await Task.Delay(1000);
        }

        if (recorder.LastSnapshot == null)
        {
            throw new HttpException(System.Net.HttpStatusCode.RequestTimeout, "Data timeout.");
        }

        var result = snapshot?.Value;
        if (string.IsNullOrEmpty(result) || result == "null")
        {
            return "";
        }

        return result;
    }
}