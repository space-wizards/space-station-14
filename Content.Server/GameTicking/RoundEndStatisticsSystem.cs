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
            SubscribeNetworkEvent<IncrementStatsValueEvent>(IncrementValue);
            SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
            SubscribeLocalEvent<RoundStatisticsAppendEvent>(OnRoundEndText);
        }

        private Dictionary<string, int> Statistics = new Dictionary<string, int>
        {
            { "ExampleCount", 0 },
        };

        public void ChangeValue(ChangeStatsValueEvent ev)
        {
            // Adds 1 to specific int
            Statistics[ev.Key] += ev.Amount;
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
            // Cream pied
            if(Statistics["ExampleCount"] != 0)
            {
                // var example = new StringBuilder();
                // example.AppendLine(Loc.GetString("example", ("count", Statistics["ExampleCount"])));
                // ev.AddLine(example.AppendLine().ToString());
            }
        }
    }
}
