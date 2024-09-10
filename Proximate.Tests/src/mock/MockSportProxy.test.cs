using Xunit;

namespace Proximate.Tests;

public class MockSportProxyTest
{
    [Fact]
    public void GenerateAValidWeekend()
    {
        var proxy = new MockSportProxy();

        var matchStartTime = new DateTime(2024, 6, 1);
        proxy.Setup(matchStartTime);

        var result = proxy.GenerateResponse(matchStartTime);
        Assert.True(result.matches.Length == 8, "Should have eight matches");
        for (var i = 0; i < result.matches.Length; i++)
        {
            var match = result.matches[i];
            Assert.True(2 == match.competitors.Length, $"Match {i} should have 2 competitors");
            Assert.True(0 == match.competitors[0].score, "Competitor 0 should have no score at start");
            Assert.True(0 == match.competitors[1].score, "Competitor 1 should have no score at start");
        }
    }

    [Fact]
    public async Task GenerateValidJson()
    {
        var proxy = new MockSportProxy();
        var time = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        proxy.Setup(time);
        var data = await proxy.Read();
        var parsed = Json.TryParse<MockSportProxy.Response>(data[0].content);
        Assert.True(parsed != null, "Should be able to parse json");
        Assert.Equal(time.ToString(), parsed.matches[0].startTime.ToString());
    }

    [Fact]
    public async Task ProgressWeekendCorrectly()
    {
        var proxy = new MockSportProxy();

        var matchStartTime = new DateTime(2024, 6, 1);
        uint matchDurationMs = 1000;
        proxy.Setup(matchStartTime, matchDurationMs);
        proxy.SetTime(new DateTime(3000, 1, 1));

        var data = await proxy.Read();
        var endState = Json.TryParse<MockSportProxy.Response>(data[0].content);
        Assert.True(endState != null, "Should have valid state");

        var endScores = new int[endState.matches.Length * 2];
        var lastMatchEnd = matchStartTime;
        for (var i = 0; i < endState.matches.Length; i++)
        {
            var match = endState.matches[i];
            endScores[i * 2] = match.competitors[0].score;
            endScores[i * 2 + 1] = match.competitors[1].score;
        }

        for (var i = 0; i < endState.matches.Length; i++)
        {
            var match = endState.matches[i];
            var matchEndTime = match.startTime.AddMilliseconds(matchDurationMs);
            proxy.SetTime(matchEndTime);

            var content = (await proxy.Read()).LastOrDefault()?.content;
            var snapshot = Json.TryParse<MockSportProxy.Response>(content);
            Assert.True(snapshot != null, "Should have valid end state");
            var matchState = snapshot.matches[i];
            Assert.True(matchState != null, "Should have valid end state");
            Assert.Equal(matchState.competitors[0].score, endScores[i * 2]);
            Assert.Equal(matchState.competitors[1].score, endScores[i * 2 + 1]);
        }
        
        for (var i = 0; i < endState.matches.Length; i++)
        {
            var match = endState.matches[i];
            var matchMidTime = match.startTime.AddMilliseconds(matchDurationMs / 2);
            proxy.SetTime(matchMidTime);

            var content = (await proxy.Read()).LastOrDefault()?.content;
            var snapshot = Json.TryParse<MockSportProxy.Response>(content);
            Assert.True(snapshot != null, "Should have valid end state");
            var matchState = snapshot.matches[i];
            Assert.True(matchState != null, "Should have valid end state");

            for (var j = 0; j < matchState.competitors.Length; j++)
            {
                var competitor = matchState.competitors[j];
                var cumulativeScore = 0;
                var scoresThatCount = competitor.scoring.Length / 2;
                for (var k = 0; k < scoresThatCount; k++)
                {
                    cumulativeScore += competitor.scoring[k];
                }
                Assert.True(cumulativeScore >= 0, "Score should be non-negative");
                Assert.True(cumulativeScore <= endScores[i * 2 + j], "Score should be less than end score");
            }
        }

        proxy.SetTime(lastMatchEnd);
    }
}