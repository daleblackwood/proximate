using EmbedIO;
using EmbedIO.Files;

namespace Proximate;

internal class Program
{
    private static WebServer? server;

    static void SetupProxies()
    {
        var logger = new SnapshotLogger();
        var recorderService = Services.Get<RecorderService>();

        var configDir = Env.ProjectPath("config");
        var monitor = new DirMonitor(configDir);

        monitor.OnUpdate += file => {
            var id = RecorderService.ToId(file);
            Log.Info("Updating config: " + id + "...");
            var config = new Config().LoadFile(file);
            config.Id = id;
            if (config == null)
            {
                Log.Error("Invalid consumer config for file: " + file);
                return;
            }
            var proxy = ConsumerFactory.Instance.Build(config);
            if (proxy == null)
            {
                Log.Error("No consumer for file: " + file);
                return;
            }

            double.TryParse(config.Get(ApiConsumer.ApiConfigKey.API_POLL_MINS) ?? "0.25", out double pollMins);
            var pollDurationMs = (uint)(pollMins * 60000.0);

            uint.TryParse(config.Get(ApiConsumer.ApiConfigKey.API_KEY_INTERVAL) ?? "10", out uint keyInterval);

            var keyStr = $"every {pollDurationMs / 60000.0} mins, keying 1:{keyInterval}";
            
            var recorder = recorderService.TryGet(id);
            if (recorder == null)
            {
                Log.Info($"Now recording consumer: '{id}' {keyStr}...");
                recorder = new ConsumerRecorder(proxy, pollDurationMs, pollDurationMs * keyInterval);
                recorderService.Register(id, recorder);
                recorder.Start();
                logger.Register(id, recorder.Snapshotter);
            }
            else
            {
                Log.Info($"Updated consumer for recorder: '{id}' {keyStr}...");
                recorder.SetConsumer(proxy);
            }
        };
        monitor.OnRemove += file => {
            var id = RecorderService.ToId(file);
            Log.Info("Removing config: " + id + "...");
            if (recorderService.Deregister(id))
            {
                Log.Info("Removed recording: " + id + "...");
            }
        };
        monitor.Start();
    }

    static void SetupWebServer()
    {
        var port = Environment.GetEnvironmentVariable("PROX_PORT");
        if (string.IsNullOrEmpty(port))
        {
            port = "80";
        }
        Log.Info($"Running at http://0.0.0.0:{port}...");
        server = new WebServer($"http://*:{port}");
        server.WithStaticFolder("/viewer", Env.ProjectPath("public/viewer"), false, fserve => {
            fserve.WithoutContentCaching();
        });
        server.WithAction("/", HttpVerbs.Get, Endpoint.Handler);
        _ = server.RunAsync();
    }

    static void Main(string[] args)
    {
        SetupProxies();
        SetupWebServer();
        while (true)
        {
            Thread.Sleep(1000);
        }
    }
}