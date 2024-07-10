using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using RequiredAttribute = System.Text.Json.Serialization.JsonRequiredAttribute;

namespace Proximate;

public class StrictAttribute : Attribute {}

public class RequiredAttribute : Attribute {}

public static class Json
{
    private const bool DEFAULT_INDENTED = false;

    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = DEFAULT_INDENTED,
        MaxDepth = 16,
        Converters = {
            new JsonStringEnumConverter(),
            new JsonDateTimeConverter(),
            new JsonTimeSpanConverter()
        },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                static typeInfo =>
                {
                    if (typeInfo.Kind != JsonTypeInfoKind.Object)
                        return;

                    var isStrict = typeInfo.GetType().GetCustomAttribute<StrictAttribute>() != null;

                    foreach (JsonPropertyInfo propertyInfo in typeInfo.Properties)
                    {
                        if (isStrict || propertyInfo.AttributeProvider == null)
                        {
                            propertyInfo.IsRequired = true;
                            continue;
                        }
                        propertyInfo.IsRequired = propertyInfo.AttributeProvider!.GetCustomAttributes(true)!.Any(x => x.GetType() == typeof(RequiredAttribute));
                    }
                }
            }
        }
    };

    public static string Serialize<T>(T obj, bool intended = DEFAULT_INDENTED)
    {
        var options = new JsonSerializerOptions(DefaultOptions);
        options.WriteIndented = intended;
        var result = JsonSerializer.Serialize(obj, options);
        return result.Trim();
    }

    public static T Deserialize<T>(string? json)
    {
        try 
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new Exception("empty input");
            }
            T? result = JsonSerializer.Deserialize<T>(json, DefaultOptions);
            if (result == null)
            {
                throw new Exception("null output");
            }
            return result!;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to deserialize JSON: " + ex.Message);
        }
    }

    public static T? TryParse<T>(string? json)
    {
        try 
        {
            return Deserialize<T>(json);
        } 
        catch 
        {
            return default;
        }
    }
}

public class JsonStringEnumConverter : JsonConverter<Enum>
{
    public override Enum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var enumString = reader.GetString();
        if (enumString == null)
        {
            return (Enum)Enum.ToObject(typeToConvert, 0);
        }
        return (Enum)Enum.Parse(typeToConvert, enumString);
    }

    public override void Write(Utf8JsonWriter writer, Enum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class JsonDateTimeConverter : JsonConverter<DateTime>
{
    private const string DateFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();
        if (dateString == null)
        {
            return DateTime.UnixEpoch;
        }
        var date = DateTime.Parse(dateString);
        var utc = date.ToUniversalTime();
        return utc;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            value = value.ToUniversalTime();
        }
        writer.WriteStringValue(value.ToString(DateFormat));
    }
}

public class JsonTimeSpanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var timeSpanString = reader.GetString();
        if (timeSpanString == null)
        {
            return TimeSpan.Zero;
        }
        return TimeSpan.Parse(timeSpanString);
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}