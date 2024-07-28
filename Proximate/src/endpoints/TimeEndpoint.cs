
namespace Proximate;

public class TimeEndpoint : Endpoint
{
    [Get("/time")]
    public DateTime Get()
    {
        return DateTime.UtcNow;
    }
}