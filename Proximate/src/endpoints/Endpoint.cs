using System.Net;
using EmbedIO.Routing;
using EmbedIO.WebApi;

public abstract class Endpoint : WebApiController 
{
}

public class GetAttribute : RouteAttribute
{
    public GetAttribute(string path) : base(EmbedIO.HttpVerbs.Get, "/" + path) {}
}