namespace Proximate.Tests;

public class MockSportProxy : IProxy
{

    public const uint DEFAULT_MATCH_DURATION_MS = 60000 * 90;

    public const string ID = "mock-sport-proxy";
    public string Id => ID;

    public class Response
    {
        public required DateTime time { get; set;}
        public required Match[] matches { get; set;}
    }

    public enum MatchState { Pending, Live, Finished }

    public class Match
    {
        public required string sport { get; set;}
        public required DateTime startTime { get; set;}
        public required MatchState state { get; set;}
        public required Competitor[] competitors { get; set;}
    }

    public class Competitor
    {
        public required string name { get; set;}
        public required int score  { get; set;}
        public required int goals { get; set;}
        public required int points { get; set;}

        public required int[] scoring { get; set;}
    }
    public static readonly string[] TeamNames = [
        "Eagles", "Bombers", "Hawks", "Lions", 
        "Crows", "Demons", "Swans", "Cats",
        "Magpies", "Power", "Kangaroos", "Blues", 
        "Bulldogs", "Saints", "Dockers", "Tigers"
    ];


    public string LastResult { get; private set; } = string.Empty;
    public DateTime LastUpdateTime => currentTime;
    

    private DateTime rngTime = DateTime.UtcNow;
    private DateTime currentTime = DateTime.UtcNow;
    private uint matchDurationMs = DEFAULT_MATCH_DURATION_MS;

    public void Setup(DateTime startTime, uint matchDurationMs = DEFAULT_MATCH_DURATION_MS)
    {
        this.rngTime = startTime;
        this.currentTime = startTime;
        this.matchDurationMs = matchDurationMs;
    }

    public void SetTime(DateTime time)
    {
        this.currentTime = time;
    }

    public async Task<string> Update()
    {
        var response = GenerateResponse(currentTime);
        LastResult = Json.Serialize(response);
        await Task.Delay(1);
        currentTime = currentTime.AddMilliseconds(1);
        return LastResult;
    }

    public Response GenerateResponse(DateTime time)
    {
        var rng = new Random((int)(rngTime - DateTime.UnixEpoch).TotalMilliseconds);

        // generate competitors and scores
        var competitors = new List<Competitor>();
        var remainingTeams = new List<string>(TeamNames);
        do
        {
            var teamI = rng.Next(remainingTeams.Count);
            var score = rng.Next(30, 130);
            var maxGoals = score / 6;
            var goals = rng.Next(maxGoals / 2, maxGoals);
            var points = score - (goals * 6);
            var scoring = new int[goals + points];
            for (var i = 0; i < goals + points; i++)
            {
                scoring[i] = i < goals ? 6 : 1;
            }
            rng.Shuffle(scoring);
            var competitor = new Competitor
            {
                name = remainingTeams[teamI],
                score = score,
                goals = goals,
                points = points,
                scoring = scoring
            };
            competitors.Add(competitor);
            remainingTeams.RemoveAt(teamI);
        }
        while (remainingTeams.Count > 0);

        // generate matches and current scores
        var matchCount = competitors.Count / 2;
        var matches = new Match[matchCount];
        for (var i = 0; i < matchCount; i++)
        {
            var startTime = time.AddMilliseconds(matchDurationMs * i);
            var endTime = startTime.AddMilliseconds(matchDurationMs);

            var matchState = time < startTime ? MatchState.Pending :
                time > startTime.AddMilliseconds(matchDurationMs) ? MatchState.Finished :
                MatchState.Live;

            var matchCompletion = time < startTime ? 0f :
                time >= endTime ? 1f :
                (float)(time - startTime).TotalMilliseconds / matchDurationMs;

            var match = new Match
            {
                sport = "AFL",
                startTime = startTime,
                state = matchState,
                competitors = [competitors[i * 2], competitors[i * 2 + 1]]
            };

            // deduct score based on match completion
            foreach (var competitor in match.competitors)
            {
                var scoreCount = Math.Min((int)(matchCompletion * 1.2f * competitor.scoring.Length), competitor.scoring.Length);
                var scoring = competitor.scoring.Take(scoreCount).ToArray();
                var score = 0;
                var goals = 0;
                var points = 0;
                foreach (var n in scoring)
                {
                    score += n;
                    if (n == 6)
                    {
                        goals++;
                    }
                    else
                    {
                        points++;
                    }
                }
                competitor.score = score;
                competitor.goals = goals;
                competitor.points = points;
                competitor.scoring = scoring;
            }
            matches[i] = match;
        }

        return new Response { time = time, matches = matches };
    } 
}