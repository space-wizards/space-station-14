using Content.Server.GameTicking.Events;
using Content.Shared.GameTicking;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Content.Server.GameTicking
{
    /// <summary>
    /// System for count different stuff and show it in round end summary.
    /// </summary>
    public sealed class RoundEndStatisticsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<IncrementStatsValueEvent>(IncrementValue);
            SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
            SubscribeLocalEvent<RoundStatisticsAppendEvent>(OnRoundEndText);
        }

        private Dictionary<string, int> Statistics = new Dictionary<string, int>
        {
            { "MoppedTimes", 0 },
            { "SlippedTimes", 0 }
        };

        public void IncrementValue(IncrementStatsValueEvent ev)
        {
            // Adds 1 to specific int
            Statistics[ev.Key] += 1;
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
            if(Statistics["MoppedTimes"] != 0)
            {
                var mopped = new StringBuilder();
                mopped.AppendLine(Loc.GetString("round-end-statistic-mopped-times", ("count", Statistics["MoppedTimes"])));
                ev.AddLine(mopped.AppendLine().ToString());
            }

            // Slipped
            if(Statistics["SlippedTimes"] != 0)
            {
                var slipped = new StringBuilder();
                slipped.AppendLine(Loc.GetString("round-end-statistic-slipped-times", ("count", Statistics["SlippedTimes"])));
                ev.AddLine(slipped.AppendLine().ToString());
            }
        }
    }
}
