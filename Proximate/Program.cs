using System.Net;
using EmbedIO;

namespace Proximate;

internal class Program
{
    private static WebServer? server;

    static void SetupApiProxy()
    {
        Env.LoadFile(Env.ProjectPath("static/oauth-demo.config"));
        var proxy = new RestProxy(Env.Get(Env.Key.API_URL, true));
        proxy.SetAuthorizer(new RestOAuthorizer(
            Env.Get(Env.Key.API_OAUTH_URL, true), 
            Env.Get(Env.Key.API_CLIENT_ID, true), 
            Env.Get(Env.Key.API_CLIENT_SECRET, true)
        ));        
        var recorder = new ProxyRecorder(proxy, 5000);
        recorder.Start();
    }

    static void SetupServer()
    {
        server = new WebServer("http://*:5050");
        server.WithStaticFolder("/viewer", Env.ProjectPath("public/viewer"), false);
        server.WithAction("/", HttpVerbs.Get, request => Endpoint.Handler(request));
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