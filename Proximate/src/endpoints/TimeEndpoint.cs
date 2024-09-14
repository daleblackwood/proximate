
namespace Proximate;

[AccessOpen]
public class TimeEndpoint : Endpoint
{
    [Get("/time")]
    public DateTime Get()
    {
        return Timing.Now;
    }
}