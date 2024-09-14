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
            Ingress = StackUtils.GetIngress(22, 64000, ["tcp", "udp"]),
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

        var instance = new Instance(Prefix + "-inst", new() {
            Tags = StackUtils.GetTags(Prefix + "-inst"),
            VolumeTags = StackUtils.GetTags(Prefix + "-volume"),
            InstanceType = "t4g.nano",
            Ami = amiId,
            VpcSecurityGroupIds = new[] { securityGroup.Id },
            KeyName = keyPair.KeyName,
            UserData = image.ImageUri.Apply(imageUri => {        
                var installScript = StackUtils.GetDockerInstallScript(imageUri, Prefix + "-repo", [
                    "/home/ec2-user/proximate/logs:/app/logs",
                    "/home/ec2-user/proximate/config:/app/config"
                ]);
                File.WriteAllText(StackUtils.ProjectPath("artefacts/docker-install.sh"), installScript);
                return installScript;
            }),
            UserDataReplaceOnChange = true
        });

        PublicIp = instance.PublicIp;

        Output.All([instance.PublicIp, image.ImageUri]).Apply(outputs => {
            var str = $"OUTPUT_INSTANCE_IP=\"{outputs[0]}\"\nOUTPUT_IMAGE_URI=\"{outputs[1]}\"";
            File.WriteAllText(StackUtils.ProjectPath("artefacts/output.sh"), str);
            return str;
        });
    }

    [Output]
    public Output<string> PublicIp { get; set; }
}
