using EmbedIO;

namespace Proximate;

internal class Program
{
    private static WebServer? server;

    static void SetupApiProxy()
    {
        Env.LoadFile(Env.ProjectPath("oauth-demo.config"));
        var proxy = new RestProxy(Env.Get(Env.Key.API_URL, true));
        proxy.SetAuthorizer(new RestOAuthorizer(
            Env.Get(Env.Key.API_OAUTH_URL, true), 
            Env.Get(Env.Key.API_CLIENT_ID, true), 
            Env.Get(Env.Key.API_CLIENT_SECRET, true)
        ));        
        var recorder = new ProxyRecorder(proxy);
        recorder.Start();
    }

    static void SetupServer()
    {
        server = new WebServer("http://localhost:5050")
            .WithStaticFolder("/viewer", Env.ProjectPath("public/viewer"), false)
            .WithWebApi("/", cfg => {
                cfg.RegisterController<ProxyEndpoint>();
            });
        _ = server.RunAsync();
    }

    static void Main(string[] args)
    {
        SetupApiProxy();
        SetupServer();
        while (true)
        {
            Thread.Sleep(1000);
        }
    }
}