

using System.Diagnostics;
using Pulumi;
using Pulumi.Aws.Ec2;
using Pulumi.Aws.Ec2.Inputs;

public static class StackUtils
{

    public static SecurityGroupIngressArgs[] GetIngress(int[] ports)
    {
        return ports.Select(port => new SecurityGroupIngressArgs {
            Protocol = "tcp",
            FromPort = port,
            ToPort = port,
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

    public static void RunShellCommand(string command)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using (var process = Process.Start(processStartInfo))
        {
            if (process != null)
            {
                process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                process.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
            }
        }
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

    public static string GetDockerInstallScript(string repoNamePrefix)
    {
        return $@"#!/bin/bash
# Environment
export AWS_REGION={RequireEnvVar("AWS_REGION")}
export AWS_ACCESS_KEY_ID={RequireEnvVar("AWS_ACCESS_KEY_ID")}
export AWS_SECRET_ACCESS_KEY={RequireEnvVar("AWS_SECRET_ACCESS_KEY")}
export ECR_URL=$(aws ecr describe-repositories --query ""repositories[?starts_with(repositoryName, '{repoNamePrefix}')].repositoryUri | [0]"" --output text)

# Install Docker
sudo yum update -y
sudo yum install -y docker
sudo service docker start
sudo usermod -a -G docker ec2-user

# Pull the Docker image
aws ecr get-login-password | docker login --username AWS --password-stdin $ECR_URL
docker pull $ECR_URL:latest

# Run the Docker container
docker run -p 5050:5050 -d $ECR_URL:latest
";
    }
}