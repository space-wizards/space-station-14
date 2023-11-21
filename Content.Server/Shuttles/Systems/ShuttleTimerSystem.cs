using Content.Server.Shuttles.Components;
// using Content.Shared.TextScreen.Events;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.RoundEnd;
using Content.Shared.Shuttles.Systems;
using Content.Shared.TextScreen.Components;
using System.Linq;
using Content.Shared.DeviceNetwork;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

using Content.Shared.TextScreen;

// TODO:
// - emergency shuttle recall inverts timer?
// - deduplicate signaltimer with a maintainer's blessing
// - scan UI?

namespace Content.Server.Shuttles.Systems
{
    /// <summary>
    /// Controls the wallmounted timers on stations and shuttles displaying e.g. FTL duration, ETA
    /// </summary>
    public sealed class ShuttleTimerSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShuttleTimerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        }

        /// <summary>
        /// Determines if/how a broadcast packet affects this timer.
        /// All shuttle timer packets are broadcast, and subnetting is implemented by filtering timer MapUid.
        /// </summary>
        private void OnPacketReceived(EntityUid uid, ShuttleTimerComponent component, DeviceNetworkPacketEvent args)
        {
            var timerXform = Transform(uid);

            // no false positives.
            if (timerXform.MapUid == null)
                return;

            string key;
            args.Data.TryGetValue("ShuttleMap", out EntityUid? shuttleMap);
            args.Data.TryGetValue("SourceMap", out EntityUid? source);
            args.Data.TryGetValue("DestMap", out EntityUid? dest);
            args.Data.TryGetValue("Docked", out bool docked);
            string text = docked ? "ETD" : "ETA";
            if (args.Data.TryGetValue("Text", out string? label))
                text = label;

            switch (timerXform.MapUid)
            {
                // sometimes the timer transforms on FTL shuttles have the hyperspace mapuid, so matching by grid works as a fallback.
                case var local when local == shuttleMap || timerXform.GridUid == shuttleMap:
                    key = "LocalTimer";
                    break;
                case var origin when origin == source:
                    key = "SourceTimer";
                    break;
                case var remote when remote == dest:
                    key = "DestTimer";
                    break;
                default:
                    return;
            }

            if (!args.Data.TryGetValue(key, out TimeSpan duration))
                return;

            RemComp<TextScreenTimerComponent>(uid);
            _appearanceSystem.SetData(uid, TextScreenVisuals.TargetTime, _gameTiming.CurTime + duration);
            _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, new string[] { text });
        }

        public void KillAll(string? freq)
        {
            var timerQuery = AllEntityQuery<ShuttleTimerComponent, DeviceNetworkComponent>();
            while (timerQuery.MoveNext(out var uid, out var _, out var net))
            {
                if (net.TransmitFrequencyId == freq)
                {
                    RemComp<TextScreenTimerComponent>(uid);
                    _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, string.Empty);
                }
            }
        }
    }
}
