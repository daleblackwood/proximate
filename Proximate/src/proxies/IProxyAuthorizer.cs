namespace Proximate;

public interface IProxyAuthorizer
{
    bool IsAuthorized { get; }
    Task Authorize(HttpClient client);
}