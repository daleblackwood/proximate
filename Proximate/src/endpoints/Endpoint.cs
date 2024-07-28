using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using System.Text.RegularExpressions;

namespace Proximate;

public abstract class Endpoint : WebApiController 
{
    public class Payload
    {
        public Dictionary<string, string> Params = new();
        public object? Body = null;
    }

    public static string GetId(Type type)
    {
        var typeName = type?.Name;
        if (string.IsNullOrEmpty(typeName))
            throw new Exception("No type.");
        if (typeof(Endpoint).IsAssignableFrom(type) == false)
            throw new Exception("Type must be Endpoint.");
        var result = Regex.Replace(typeName, "([a-z])([A-Z])", "$1-$2").ToLower();
        return result;
    }

    public static string GetRoute(Type type)
    {
        var route = "/" + GetId(type);
        if (route.EndsWith("-endpoint")) 
        {
            route = route.Substring(0, route.Length - 9);
        }
        if (route == "/root")
        {
            return "/";
        }
        return route;
    }

    public static async Task Handler(IHttpContext request)
    {
        try
        {
            var url = request.Request.Url;
            var method = request.Request.HttpVerb;
            var route = GetRouteFromUri(request.Request.Url);
            var endpointType = GetEndpointByRoute(route);
            if (endpointType == null)
            {
                throw new HttpException(System.Net.HttpStatusCode.NotFound);
            }
            var inst = Activator.CreateInstance(endpointType) as Endpoint;
            if (inst == null)
            {
                throw new Exception("Could not handle " + endpointType.Name);
            }
            object? result = null;
            var payload = new Payload();
            switch (method)
            {
                case HttpVerbs.Get:
                    result = await inst.Get(payload);
                    break;

                default:
                    throw new HttpException(System.Net.HttpStatusCode.MethodNotAllowed);
            }

            request.Response.StatusCode = 200;
            using (var writer = request.OpenResponseText())
            {
                var str = result is string sResult ? sResult : Json.Serialize(result, true);
                await writer.WriteAsync(str);
            }
        }
        catch (Exception e)
        {
            var code = 400;
            if (e is HttpException he)
            {
                code = he.StatusCode;
            }
            request.Response.StatusCode = code;
            var errorDesc = $"{code} ({Enum.GetName(typeof(System.Net.HttpStatusCode), code) ?? "Unknown"}): {e.Message}";
            Console.WriteLine($"{errorDesc}:\n{e.StackTrace ?? ""}");
            using (var writer = request.OpenResponseText())
            {
                await writer.WriteAsync(errorDesc);
            }
        }
    }

    private static Type[]? cahcedTypes = null;

    public static Type[] GetAllTypes()
    {
        if (cahcedTypes == null)
        {
            cahcedTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.IsSubclassOf(typeof(Endpoint)))
                .ToArray();
        }
        return cahcedTypes;
    }

    public static string GetRouteFromUri(Uri uri)
    {
        var path = uri.AbsolutePath;
        if (path[0] == '/')
        {
            path = path.Substring(1);
        }
        var route = "/" + path.Split("/")[0];
        return route;
    }

    public static Type? GetEndpointByRoute(string route)
    {
        var types = GetAllTypes();
        return types.FirstOrDefault(x => GetRoute(x) == route);
    }

    public virtual Task<object> Get(Payload payload) => throw new HttpException(System.Net.HttpStatusCode.MethodNotAllowed);
}

public class GetAttribute : RouteAttribute
{
    public GetAttribute(string path) : base(EmbedIO.HttpVerbs.Get, path) {}
}