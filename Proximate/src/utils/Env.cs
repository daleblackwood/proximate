namespace Proximate;

public static class Env
{
    public enum Key
    {
        API_URL,
        API_OAUTH_URL,
        API_CLIENT_ID,
        API_CLIENT_SECRET,
        API_AUDIENCE
    }

    public enum OperationMode
    {
        Development,
        Testing,
        Production
    }

    public static void SetOperationMode(OperationMode mode)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", mode.ToString());
    }

    public static OperationMode GetOperationMode()
    {
        var mode = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (mode == "Production")
        {
            return OperationMode.Production;
        }
        else if (mode == "Testing")
        {
            return OperationMode.Testing;
        }
        else
        {
            return OperationMode.Development;
        }
    }
    
    public static string? Get(string key, bool required = false)
    {
        var value = Environment.GetEnvironmentVariable(key);
        var isEmpty = string.IsNullOrWhiteSpace(value);
        if (isEmpty && required)
        {
            throw new Exception($"Missing environment variable: {key}");
        }
        return isEmpty ? null : value;
    }

    public static string Get(Key key, bool required = false)
    {
        return Get(key.ToString(), required);
    }

    public static void LoadFile(string file)
    {
        if (File.Exists(file) == false)
        {
            throw new Exception($"File not found: {file}");
        }
        var text = File.ReadAllText(file);
        LoadText(text);
    }

    public static void LoadText(string text)
    {
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            var parts = line.Split('=', 2);
            var key = parts[0].Trim();
            var value = parts.Length > 1 ? parts[1].Trim() : "";
            if (string.IsNullOrEmpty(value) == false)
            {
                if (value.StartsWith("\"") && value.EndsWith("\""))
                {
                    value = value.Substring(1, value.Length - 2);
                }
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    public static Dictionary<string, string> LoadConfigFile(string path)
    {
        var str = File.ReadAllText(ProjectPath(path));
        return ParseConfig(str);
    }
    
    public static Dictionary<string, string> ParseConfig(string text)
    {
        var result = new Dictionary<string, string>();
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            var parts = line.Split('=', 2);
            var key = parts[0].Trim();
            var value = parts.Length > 1 ? parts[1].Trim() : "";
            if (string.IsNullOrEmpty(value) == false)
            {
                if (value.StartsWith("\"") && value.EndsWith("\""))
                {
                    value = value.Substring(1, value.Length - 2);
                }
                result[key] = value;
            }
        }
        return result;
    }

    private static string? root = null;

    public static string ProjectPath(string subpath = "")
    {
        if (root == null)
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var binI = basePath.IndexOf("bin");
            if (binI >= 0)
            {
                root = basePath.Substring(0, binI);
            }
            else
            {
                root = basePath;
            }
        }       
        return Path.GetFullPath(Path.Combine(root, subpath));
    }
}