

using System.Collections.Immutable;
using System.Diagnostics;
using Pulumi;
using Pulumi.Aws.Ec2;
using Pulumi.Aws.Ec2.Inputs;

public static class StackUtils
{

    public static readonly string[] DEFAULT_PROTOCOLS = ["tcp"]; 

    public static SecurityGroupIngressArgs[] GetIngress(int[] ports, string[]? protocols = null)
    {
        if (protocols == null)
        {
            protocols = DEFAULT_PROTOCOLS;
        }
        return ports.SelectMany(port => GetIngress(port, port, protocols)).ToArray();
    }

    public static SecurityGroupIngressArgs[] GetIngress(int fromPort, int toPort, string[]? protocols = null)
    {
        if (protocols == null)
        {
            protocols = DEFAULT_PROTOCOLS;
        }
        return protocols.Select(protocol => new SecurityGroupIngressArgs {
            Protocol = protocol,
            FromPort = fromPort,
            ToPort = toPort,
            CidrBlocks = new List<string> { "0.0.0.0/0" }
        }).ToArray();
    }

    public static SecurityGroupEgressArgs[] GetEgress()
    {
        return [new() {
            Protocol = "-1",
            FromPort = 0,
            ToPort = 0,
            CidrBlocks = new List<string> { "0.0.0.0/0" }
        }];
    }

    private static string? projDir = null;

    public static string ProjectPath(string relPath = "") 
    {
        if (projDir == null)
        {
            projDir = Environment.CurrentDirectory;
            var binI = projDir.IndexOf("/bin/");
            if (binI > 0)
            {
                projDir = projDir.Substring(0, projDir.IndexOf("/bin/"));
            }
        }
        return Path.Join(projDir, relPath);
    }

    public static InputMap<string> GetTags(string name)
    {
        return new InputMap<string> { { "Name", name } };
    }

    public static KeyPair CreateKeyPair(string name) 
    {
        return new KeyPair(name, new KeyPairArgs {
            PublicKey = "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIJ9nbPxWfv9Ci8y/iA77PrAA6gwb9e3JAqovow1izZ3M deployment@completelycooked.com"
        });
    }

    public static string RequireEnvVar(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new Exception("Required environment variable '" + key + "' missing.");
        }
        return value;
    }

    public static string GetDockerInstallScript(string imageUrl, string repoNamePrefix, string[]? volumes = null)
    {
        var volumeStr = "";
        var mkdirStr = "";
        if (volumes != null)
        {
            for (var i=0; i<volumes.Length; i++)
            {
                var path = volumes[i];
                var pathBits = path.Split(':');
                if (pathBits.Length != 2)
                {
                    throw new Exception("Invalid path: " + path);
                }
                foreach (var pathBit in pathBits)
                {
                    if (pathBit.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                    {
                        throw new Exception("Invalid path: " + path);
                    }
                    if (Path.IsPathRooted(pathBit) == false)
                    {
                        throw new Exception("Must be absolute path: " + path);
                    }
                }
                volumeStr += "--volume " + path + " ";
                mkdirStr += "sudo mkdir -p \"" + pathBits[0] + "\"\n";
            }
        }
        return $@"#!/bin/bash
set -e
# Environment
export AWS_REGION={RequireEnvVar("AWS_REGION")}
export AWS_ACCESS_KEY_ID={RequireEnvVar("AWS_ACCESS_KEY_ID")}
export AWS_SECRET_ACCESS_KEY={RequireEnvVar("AWS_SECRET_ACCESS_KEY")}
export IMAGE_URL=""{imageUrl}""
export ECR_URL=$(aws ecr describe-repositories --query ""repositories[?starts_with(repositoryName, '{repoNamePrefix}')].repositoryUri | [0]"" --output text)

# Install Docker
sudo yum update -y
sudo yum install -y docker
sudo service docker start
sudo usermod -a -G docker ec2-user

# Pull the Docker image
aws ecr get-login-password | docker login --username AWS --password-stdin $ECR_URL
docker pull $ECR_URL:latest

{mkdirStr}
# Run the Docker container
docker run {volumeStr} --network host -d $ECR_URL:latest
";
    }

    public static string GenerateBucketConfig(string bucketName) {
        return @$"
AWS_REGION={RequireEnvVar("AWS_REGION")}
AWS_ACCESS_KEY_ID={RequireEnvVar("AWS_ACCESS_KEY_ID")}
AWS_SECRET_ACCESS_KEY={RequireEnvVar("AWS_SECRET_ACCESS_KEY")}
S3_BUCKET_NAME={bucketName} 
";
    }

    public static void OutputMap<T>(Dictionary<string, Input<T>> map, Action<Dictionary<string, T>> callback) {
        var keys = map.Keys.ToArray();
        var values = ImmutableArray.Create(map.Values.ToArray());
        Output.All<T>(values).Apply(outputs => {
            var dict = new Dictionary<string, T>();
            for (var i = 0; i < values.Length; i++) 
            {
                dict[keys[i]] = outputs[i];
            }
            callback(dict);
            return dict;
        });
    }
}