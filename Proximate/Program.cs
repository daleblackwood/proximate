using System.Net;
using EmbedIO;
using EmbedIO.Files;

namespace Proximate;

internal class Program
{
    private static WebServer? server;

    static void SetupProxies()
    {
        var configDir = Env.ProjectPath("config");
        foreach (var file in Directory.GetFiles(configDir))
        {
            var key = Path.GetFileNameWithoutExtension(file).ToLower();
            var config = ProxyConfig.Load(file);
            var proxy = config.CreateRestProxy();
            var recorder = new ProxyRecorder(proxy, 5000);
            recorder.Start();
            Services.Get<ProxyRecorderService>().Register(key, recorder);
            Log.Info("Started recording " + key + "...");
        }
    }

    static void SetupServer()
    {
        var port = 5050;
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
        SetupServer();
        while (true)
        {
            Thread.Sleep(1000);
        }
    }
}