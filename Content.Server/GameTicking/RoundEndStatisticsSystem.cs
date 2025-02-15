using Content.Server.GameTicking.Events;
using Content.Shared.GameTicking;
using System.Linq;

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
            SubscribeLocalEvent<ChangeStatsValueEvent>(ChangeValue);
            SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
            SubscribeLocalEvent<RoundStatisticsAppendEvent>(OnRoundEndText);
        }

        private Dictionary<string, int> _statistics = new Dictionary<string, int>
        {
            { "SlippedCount", 0 },
            { "CreamedCount", 0 },
            { "MoppedCount", 0},
        };

        // Change the value by given int
        public void ChangeValue(ChangeStatsValueEvent args)
        {
            if (args.Handled)
                return;

            if (_statistics.TryGetValue(args.Key, out var currentValue))
            {
                _statistics[args.Key] = currentValue + args.Amount;
            }

            args.Handled = true;
        }

        // Set all ints to zero
        private void OnRoundStart(RoundStartingEvent args)
        {
            foreach (var key in _statistics.Keys.ToList())
            {
                _statistics[key] = 0;
            }
        }

        private void OnRoundEndText(RoundStatisticsAppendEvent args)
        {
            foreach (var (key, value) in _statistics)
            {
                if (value <= 0)
                    continue;

                var text = Loc.GetString($"round-end-statistic-{key.ToLower()}", ("count", value));
                args.AddLine(text);
            }
        }
    }
}
