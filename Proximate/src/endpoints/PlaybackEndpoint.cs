using EmbedIO;
using Proximate;

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
            context.SetHeader("Head", Format.DateTime(snapshot.Time.AddSeconds(-delay)));
        }

        context.SetHeader("First", Format.DateTime(recorder.FirstUpdateTime));
        context.SetHeader("Last", Format.DateTime(recorder.LastSnapshot?.Time));
        context.SetHeader("Polled", Format.DateTime(recorder.LastUpdateTime));
        context.SetHeader("Storage", $"{Math.Ceiling((double)(recorder.StorageSize / 1024) / 1024 * 100) / 100.0}mb");
        context.SetHeader("Keys", recorder.KeyshotCount.ToString());
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