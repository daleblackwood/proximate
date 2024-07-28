using System.Diagnostics;
using Pulumi;
using Pulumi.Aws.Ec2;
using Pulumi.Aws.Ec2.Inputs;
using Pulumi.Aws.ImageBuilder;
using Awsx = Pulumi.Awsx;

namespace Proximate.Deploy;

class ProximateStack : Stack
{
    public string Prefix { get; private set; }

    public ProximateStack()
    {
        Prefix = (Deployment.Instance.ProjectName + "-" + Deployment.Instance.StackName).ToLower();

        var keyPair = StackUtils.CreateKeyPair(Prefix + "-key");

        var securityGroup = new SecurityGroup(Prefix + "-sg", new() {
            Description = "Proximate Security Group",
            Ingress = StackUtils.GetIngress([22, 80, 443, 5050]),
            Egress = StackUtils.GetEgress()
        });

        var ami = GetAmi.InvokeAsync(new() {
            Owners = { "amazon" },
            Filters =  { new GetAmiFilterArgs { Name = "name", Values = { "*-ami-*-arm64" } } },
            MostRecent = true,
        }).Result;
        var amiId = ami.Id;

        Console.WriteLine("ami", ami.Name);

        var ecr = new Awsx.Ecr.Repository(Prefix + "-repo", new() {
            ForceDelete = true
        });
        var image = new Awsx.Ecr.Image(Prefix + "-image", new Awsx.Ecr.ImageArgs() {
            RepositoryUrl = ecr.Url,
            Context = "../Proximate",
            Platform = "linux/arm64",
            ImageTag = "latest"
        });
        
        var installScript = StackUtils.GetDockerInstallScript(Prefix + "-repo");
        File.WriteAllText(StackUtils.ProjectPath("artefacts/docker-install.sh"), installScript);

        var instance = new Instance(Prefix + "-inst", new() {
            Tags = StackUtils.GetTags(Prefix + "-inst"),
            VolumeTags = StackUtils.GetTags(Prefix + "-volume"),
            InstanceType = "t4g.nano",
            Ami = amiId,
            VpcSecurityGroupIds = new[] { securityGroup.Id },
            KeyName = keyPair.KeyName,
            UserData = installScript,
            UserDataReplaceOnChange = true
        });

        PublicIp = instance.PublicIp;
    }

    [Output]
    public Output<string> PublicIp { get; set; }
}
