using System.Net;
using EmbedIO;
using EmbedIO.Files;

namespace Proximate;

internal class Program
{
    private static WebServer? server;

    static void SetupProxies()
    {
        var recorderService = Services.Get<RecorderService>();
        var logger = Services.Get<SnapshotLogService>();

        var configDir = Env.ProjectPath("config");
        var monitor = new DirMonitor(configDir, ".toml");

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

            var pollInterval = Config.ParseSpan(config.Get(ApiConsumer.ApiConfigKey.POLL_INTERVAL), TimeSpan.FromMinutes(1));
            var keyInterval = Config.ParseSpan(config.Get(ApiConsumer.ApiConfigKey.KEY_INTERVAL), TimeSpan.FromMinutes(30));

            var keyStr = $"every {pollInterval.TotalMinutes.ToString("0.0")} mins, keying every {keyInterval.TotalMinutes.ToString("0.0")} mins";
            
            var recorder = recorderService.TryGet(id);
            if (recorder == null)
            {
                Log.Info($"Now recording consumer: '{id}' {keyStr}...");
                recorder = new ConsumerRecorder(proxy, pollInterval, keyInterval);
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
        Log.Info($"Proximate init.");
        var environmentName = Env.GetOperationMode().ToString() + " " + IPAddress.Any.ToString();
        if (Env.IsTestEnvironment == false)
        {
            SentrySdk.Init(options => {
                options.Dsn = "https://d850786f0dd55f313242ba791608989f@o4508956393340928.ingest.us.sentry.io/4508956398256128";
                options.Debug = false;
                options.AutoSessionTracking = true;
                options.Environment = environmentName;
            });
        }
        Log.Info($"Running Proximate as {environmentName}...");
        SetupProxies();
        SetupWebServer();
        while (true)
        {
            Thread.Sleep(1000);
        }
    }
}