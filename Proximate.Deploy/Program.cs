using Pulumi;

namespace Proximate.Deploy;

class Program
{
    static Task<int> Main(string[] args) 
    {
        return Deployment.RunAsync<ProximateDockerStack>();
    }
}