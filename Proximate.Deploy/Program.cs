using Pulumi;

namespace Proximate.Deploy;

class Program
{
    static Task<int> Main(string[] args) => Deployment.RunAsync<ProximateStack>();
}