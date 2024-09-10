namespace Proximate;


public class Config
{
    protected Dictionary<string, string> values = new();

    public string? Id { get; set; }

    protected void Set(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new KeyNotFoundException("Invalid key");
        }
        if (string.IsNullOrWhiteSpace(value) == false)
        {
            values[key] = value;
        }
    }

    public void Set(Dictionary<string, string> newValues) 
    {
        foreach (var pair in newValues)
        {
            Set(pair.Key, pair.Value);
        }
        Validate();
    }

    public virtual void Validate() {}

    public string? Get(string key)
    {
        if (values.ContainsKey(key))
        {
            var value = values[key];
            if (string.IsNullOrWhiteSpace(value) == false)
            {
                return value;
            }
        }
        return null;
    }

    public string? Get<TKey>(TKey key) where TKey : struct, Enum => Get(Enum.GetName(key)!);

    public bool Has(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (Get(key) == null)
                return false;
        }
        return true;
    }

    public bool Has<TKey>(params TKey[] keys) where TKey : struct, Enum
    {
        foreach (var key in keys)
        {
            var keyStr = Enum.GetName<TKey>(key);
            if (string.IsNullOrEmpty(keyStr))
                return false;
            if (Has(keyStr) == false)
                return false;
        }
        return true;
    }

    public Config LoadFile(string path)
    {
        var text = File.ReadAllText(Env.ProjectPath(path));
        Set(ParseConfig(text));
        return this;
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

    public override int GetHashCode()
    {
        var result = 0;
        foreach (var value in values.Values)
        {
            if (string.IsNullOrEmpty(value) == false)
            {
                result += value.GetHashCode();
            }
        }
        return result;
    }
}

public class Config<TKey> : Config where TKey : struct, Enum
{
    private static string ToKey(TKey key) => Enum.GetName(key) ?? throw new KeyNotFoundException("Invalid key for " + typeof(TKey).Name);

    public void Set(TKey key, string value)
    {
        var keyStr = Enum.GetName(key);
        if (string.IsNullOrWhiteSpace(keyStr) == false)
        {
            Set(keyStr, value);
        }
    }

    public void Copy(Config config)
    {
        foreach (var key in Enum.GetNames(typeof(TKey)))
        {
            var value = config.Get(key);
            if (value != null)
            {
                Set(key, value);
            }
        }
    }

    public string? Get(TKey key) => Get(ToKey(key));

    public string Require(TKey key) => Get(ToKey(key)) ?? throw new KeyNotFoundException("Missing key " + ToKey(key));

    public bool Has(params TKey[] keys) => base.Has<TKey>(keys);
    
}