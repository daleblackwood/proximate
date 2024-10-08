using Pulumi;
using Pulumi.Aws.S3;
using Pulumi.Aws.Ec2;
using Pulumi.Aws.Ec2.Inputs;
using Awsx = Pulumi.Awsx;

namespace Proximate.Deploy;

class ProximateNetStack : Stack
{
    public string Prefix { get; private set; }

    public ProximateNetStack()
    {
        Prefix = (Deployment.Instance.ProjectName + "-" + Deployment.Instance.StackName).ToLower();

        var keyPair = StackUtils.CreateKeyPair(Prefix + "-key");

        var securityGroup = new SecurityGroup(Prefix + "-sg", new() {
            Description = "Proximate Security Group",
            Ingress = StackUtils.GetIngress(22, 64000, ["tcp", "udp"]),
            Egress = StackUtils.GetEgress()
        });

        var bucket = new Bucket(Prefix + "-bucket", new () {
            Tags = StackUtils.GetTags(Prefix + "-bucket")
        });

        var ami = GetAmi.InvokeAsync(new() {
            Owners = { "amazon" },
            Filters =  { new GetAmiFilterArgs { Name = "name", Values = { "*-ami-*-arm64" } } },
            MostRecent = true,
        }).Result;
        var amiId = ami.Id;

        var instance = new Instance(Prefix + "-inst", new() {
            Tags = StackUtils.GetTags(Prefix + "-inst"),
            VolumeTags = StackUtils.GetTags(Prefix + "-volume"),
            InstanceType = "t4g.micro",
            Ami = amiId,
            VpcSecurityGroupIds = new[] { securityGroup.Id },
            KeyName = keyPair.KeyName,
            UserDataReplaceOnChange = true
        });

        var elasticIp = new Eip(Prefix + "-ip", EipArgs.Empty);
        var eipAssociation = new EipAssociation(Prefix + "-ip-map", new () {
            InstanceId = instance.Id,
            AllocationId = elasticIp.Id
        });

        PublicIp = elasticIp.PublicIp;

        StackUtils.OutputMap<string>(new () {
            { "DEPLOYMENT_STACK", Output.Format($"{Deployment.Instance.StackName}") },
            { "DEPLOYMENT_PROJECT", Output.Format($"{Deployment.Instance.ProjectName}") },
            { "DEPLOYMENT_INSTANCE_IP", elasticIp.PublicIp },
            { "DEPLOYMENT_BUCKET_NAME", bucket.BucketName }
        }, outputs => {
            var str = "";
            foreach (var pair in outputs) {
                if (string.IsNullOrWhiteSpace(pair.Value)) continue;
                str += $"export {pair.Key}=\"{pair.Value}\"\n";
            }
            File.WriteAllText(StackUtils.ProjectPath($"artefacts/{Deployment.Instance.StackName}.deployment.sh"), str);
            File.WriteAllText(StackUtils.ProjectPath("artefacts/s3.config"), StackUtils.GenerateBucketConfig(outputs["DEPLOYMENT_BUCKET_NAME"]));
        });
    }

    [Output]
    public Output<string> PublicIp { get; set; }
}
