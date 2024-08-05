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
            SubscribeNetworkEvent<ChangeStatsValueEvent>(ChangeValue);
            SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
            SubscribeLocalEvent<RoundStatisticsAppendEvent>(OnRoundEndText);
        }

        private Dictionary<string, int> Statistics = new Dictionary<string, int>
        {
            { "ExampleCount", 0 },
        };

        // Change the value by given int
        public void ChangeValue(ChangeStatsValueEvent ev)
        {
            Statistics[ev.Key] += ev.Amount;
        }

        // Set all ints to zero
        private void OnRoundStart(RoundStartingEvent ev)
        {
            foreach (var key in Statistics.Keys.ToList())
            {
                Statistics[key] = 0;
            }
        }

        private void OnRoundEndText(RoundStatisticsAppendEvent ev)
        {
            // Don't add anything if 0. We don't want to spoil any specific and hard statistics.
            if(Statistics["ExampleCount"] != 0)
            {
                // var example = new StringBuilder();
                // example.AppendLine(Loc.GetString("example", ("count", Statistics["ExampleCount"])));
                // ev.AddLine(example.AppendLine().ToString());
            }
        }
    }
}
