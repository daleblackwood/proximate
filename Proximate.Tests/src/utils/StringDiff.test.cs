using Xunit;

namespace Proximate.Tests;

public class StringDiffTest
{ 
    [Fact]
    public void ShouldApplySimpleDiffs()
    {
        string a = "the quick brown fox";
        string b = "the slow brown dog";
        var diffs = StringDiff.Calculate(a, b);
        string c = StringDiff.Apply(a, diffs);
        Assert.Equal(b, c);
    }

    struct OperateSimpleJsonData
    {
        public string name { get; set; }
        public string[] labels { get; set; }
        public int[] values { get; set; }
    };

    
    [Fact]
    public void ShouldOperateOnSimpleJson()
    {
        var a = new OperateSimpleJsonData
        {
            name = "Named Thing",
            labels = ["a", "b", "c"],
            values = [1, 2, 3]
        };
        var strA = Json.Serialize(a);
        var b = new OperateSimpleJsonData
        {
            name = "Named Thing",
            labels = ["a", "b", "d"],
            values = [1, 4, 3]
        };
        var strB = Json.Serialize(b);

        var diffs = StringDiff.Calculate(strA, strB);
        var strPatched = StringDiff.Apply(strA, diffs);
        Assert.True(strB == strPatched, "Patched string should match target string");

        var c = Json.Deserialize<OperateSimpleJsonData>(strPatched);
        Assert.True(b.labels[2] == c.labels[2], "Patched label should match target label");
        Assert.True(b.values[1] == c.values[1], "Patched value should match target value");
    }
    
    [Fact]
    public void ShouldOperateOnComplexJsonSets()
    {
        long tsA = new DateTime(2025, 1, 1).Ticks;
        var a = new
        {
            time = new DateTime(tsA).ToString("o"),
            data = new List<object>(),
            extra = new { custom = true }
        };
        long tsB = tsA + TimeSpan.FromHours(1).Ticks;
        var b = new
        {
            time = new DateTime(tsB).ToString("o"),
            data = new List<object>(),
            extra = new { custom = true, fanciness = new[] { 0.745, 233 } }
        };

        var names = new[] { "Jason", "Tim", "Samuel", "Alexander", "J" };
        for (int i = 0; i < 20; i++)
        {
            var dataA = new { name = $"Name {names[i % names.Length]} {names[(i * 7) % names.Length]}", score = i * 100 };
            var dataB = new { name = dataA.name, score = i * 200 };
            a.data.Add(dataA);
            b.data.Add(i % 3 == 0 ? dataB : dataA);
        }

        var strA = Json.Serialize(a);
        var strB = Json.Serialize(b);
        var diffs = StringDiff.Calculate(strA, strB);
        var strPatched = StringDiff.Apply(strA, diffs);
        Assert.True(strB == strPatched, "Patched string should match target string");
    }
}
