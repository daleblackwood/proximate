namespace Proximate;

public interface IAuthorizer
{
    bool IsAuthorized { get; }
    Task Authorize(HttpClient client);
}