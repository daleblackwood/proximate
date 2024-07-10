using System.Text.Json.Serialization;
using Xunit;

namespace Proximate.Tests;

public class JsonTest
{
    class JsonTestData
    {
        [Required]
        public DateTime time { get; set; }
    }

    [Fact]
    public void ThrowsBadTypes()
    {
        var invalidJson = "invalid-json";
        var validJson = "{ \"a\": 1 }";
        Assert.ThrowsAny<Exception>(() => Json.Deserialize<JsonTestData>(invalidJson));
        Assert.Null(Json.TryParse<JsonTestData>(invalidJson));
        Assert.ThrowsAny<Exception>(() => Json.Deserialize<JsonTestData>(validJson));
        Assert.Null(Json.TryParse<JsonTestData>(validJson));
    }


    [Fact]
    public void DeserializeJson()
    {
        var str = "{\"time\":\"2025-01-01T00:00:00.000Z\"}";
        var a = Json.TryParse<JsonTestData>(str);
        Assert.True(a != null, "Should be able to parse json");
        Json.Deserialize<JsonTestData>(str);
        Assert.True(a.time.Kind == DateTimeKind.Utc, "Time should be in UTC");
        Assert.Equal(2025, a.time.Year);
        Assert.Equal(1, a.time.Month);
        Assert.Equal(1, a.time.Day);
        Assert.Equal(0, a.time.Hour);
        Assert.Equal(0, a.time.Minute);
        Assert.Equal(0, a.time.Second);
    }

    [Fact]
    public void SerializeJson()
    {
        var date = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var json = Json.Serialize(new JsonTestData { time = date }, false);
        Assert.Equal("{\"time\":\"2025-01-01T00:00:00.000Z\"}", json);
    }

}