using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using System.Collections.Specialized;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Proximate;

public abstract class Endpoint : WebApiController 
{
    public class EndpointContext
    {
        public readonly Uri Url;
        public string? Body { get; set; }
        public NameValueCollection RequestHeaders { get; set; } = new();
        
        private Dictionary<string, string> parameters = new();
        private Dictionary<string, string> headers = new();

        public EndpointContext(Uri url)
        {
            this.Url = url;
            if (string.IsNullOrEmpty(url.Query) == false)
            {
                var query = url.Query.Trim().Substring(1);
                var parts = query.Split("&");
                foreach (var part in parts) {
                    var pair = part.Split("=");
                    var key = pair[0].ToLower().Trim();
                    parameters[key] = pair.Length > 0 ? pair[1].Trim() : "true";
                }
            }
        }

        public string? GetParameter(string key, string? fallbackValue = null)
        {
            key = key.ToLower().Trim();
            if (parameters.ContainsKey(key))
            {
                return parameters[key];
            }
            return fallbackValue;
        }

        public string? GetPath(int index = -1)
        {
            var path = Url.LocalPath?.Trim();
            if (string.IsNullOrEmpty(path) == false && path.Length > 1 && path[0] == '/')
            {
                path = path.Substring(1);
            }
            if (index >= 0 && string.IsNullOrEmpty(path) == false)
            {
                var pathBits = path?.Split("/");
                path = pathBits != null && index < pathBits.Length ? pathBits[index] : null;
            }
            return string.IsNullOrEmpty(path) ? null : path;
        }
        
        public double GetParameterNumber(string key, double fallbackValue = 0.0)
        {
            var str = GetParameter(key);
            return str == null ? fallbackValue : double.Parse(str);
        }

        public T? GetData<T>()
        {
            return Json.TryParse<T>(Body);
        }

        public bool GetParameterBool(string key, bool fallbackValue = false)
        {
            var str = GetParameter(key);
            return (str != null && str.Trim().ToLower() == "true") || fallbackValue;
        }

        public void SetHeader(string key, string value)
        {
            key = key.Trim();
            headers[key] = value;
        }

        public void WriteHeaders(WebHeaderCollection outHeaders)
        {
            foreach (var pair in headers)
            {
                outHeaders.Add(pair.Key, pair.Value);
            }
        }
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
                throw new HttpException(HttpStatusCode.NotFound);
            }
            var accessRequired = AccessLevel.Open;
            var accessArribute = endpointType.GetCustomAttribute<AccessAttribute>();
            if (accessArribute != null)
            {
                accessRequired = accessArribute.level;
            }
            var inst = Activator.CreateInstance(endpointType) as Endpoint;
            if (inst == null)
            {
                throw new Exception("Could not handle " + endpointType.Name);
            }
            object? result = null;
            var context = new EndpointContext(url);
            context.RequestHeaders = request.Request.Headers;
            if (request.Request.HasEntityBody)
            {
                using var reader = new StreamReader(request.Request.InputStream, Encoding.UTF8);
                context.Body = await reader.ReadToEndAsync();
            }

            Func<EndpointContext, Task<object>>? methodFunc = null;
            switch (method)
            {
                case HttpVerbs.Get:
                    methodFunc = inst.Get;
                    break;

                case HttpVerbs.Post:
                    methodFunc = inst.Post;
                    break;
            }

            if (methodFunc != null)
            {
                var methodAccessAttribute = methodFunc.GetMethodInfo().GetCustomAttribute<AccessAttribute>();
                if (methodAccessAttribute != null)
                {
                    if (methodAccessAttribute.level > accessRequired)
                    {
                        accessRequired = methodAccessAttribute.level;
                    }
                }
                if (accessRequired > AccessLevel.Open)
                {
                    try {
                        var token = AuthUtils.ExtractBearerToken(request.Request.Headers);
                        var accessAllowed = await Services.Get<AuthService>().CanAccess(token, accessRequired);
                        if (accessAllowed == false)
                        {
                            throw new Exception("Access denied.");
                        }
                    }
                    catch (Exception e)
                    {
                        throw new HttpException(HttpStatusCode.Unauthorized, e.Message);
                    }
                }
                result = await methodFunc.Invoke(context);
            }
            else
            {
                throw new HttpException(HttpStatusCode.MethodNotAllowed);
            }

            context.WriteHeaders(request.Response.Headers);
            request.Response.StatusCode = 200;

            await using (var writer = request.OpenResponseText())
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
            var errorDesc = $"{code} ({Enum.GetName(typeof(HttpStatusCode), code) ?? "Unknown"}): {e.Message}";
            if (code != 400)
            {
                Log.Info("Request error: " + errorDesc);
            }
            else 
            {
                Log.Error($"{errorDesc}:\n{e.StackTrace ?? ""}");
            }
            using (var writer = request.OpenResponseText())
            {
                await writer.WriteAsync(errorDesc);
            }
        }
    }

    private static Type[]? cachedTypes = null;

    public static Type[] GetAllTypes()
    {
        if (cachedTypes == null)
        {
            cachedTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.IsSubclassOf(typeof(Endpoint)))
                .ToArray();
        }
        return cachedTypes;
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

    public virtual Task<object> Get(EndpointContext payload) => throw new HttpException(HttpStatusCode.MethodNotAllowed);

    public virtual Task<object> Post(EndpointContext payload) => throw new HttpException(HttpStatusCode.MethodNotAllowed);
}

public class GetAttribute : RouteAttribute
{
    public GetAttribute(string path) : base(EmbedIO.HttpVerbs.Get, path) {}
}