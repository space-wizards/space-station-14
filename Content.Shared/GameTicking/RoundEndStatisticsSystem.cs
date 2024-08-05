using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using System.Numerics;
using System.Text;

namespace Content.Shared.GameTicking
{
    /// <summary>
    /// System for count different stuff and show it in round end summary.
    /// </summary>
    public sealed class RoundEndStatisticsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
            SubscribeLocalEvent<RoundStatisticsAppendEvent>(OnRoundEndText);
        }

        Dictionary<string, int> Statistics = new Dictionary<string, int>
        {
            { "MoppedTimes", 0 },
            { "SlippedTimes", 0 }
        };

        public void IncrementStatsValue(string key)
        {
            // Adds 1 to specific int
            Statistics[key]++;
        }

        private void OnRoundStart(RoundStartingEvent ev)
        {
            // Set all ints to zero
            foreach (var key in Statistics.Keys.ToList())
            {
                Statistics[key] = 0;
            }
        }

        private void OnRoundEndText(RoundStatisticsAppendEvent ev)
        {
            // Mopped
            if(MoppedTimes != 0)
            {
                var mopped = new StringBuilder();
                mopped.AppendLine(Loc.GetString("round-end-statistic-mopped-times", ("count", count)));
                ev.AddLine(mopped.AppendLine().ToString());
            }

            // Slipped
            if(SlippedTimes != 0)
            {
                var slipped = new StringBuilder();
                slipped.AppendLine(Loc.GetString("round-end-statistic-slipped-times", ("count", count)));
                ev.AddLine(slipped.AppendLine().ToString());
            }
        }
    }
}
