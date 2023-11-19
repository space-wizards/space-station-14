using Content.Server.Shuttles.Components;
using Content.Shared.TextScreen.Events;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.RoundEnd;
using Content.Shared.Shuttles.Systems;
using Content.Shared.TextScreen.Components;
using System.Linq;
using Content.Shared.DeviceNetwork;
using Robust.Shared.GameObjects;
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
        // [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;


        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShuttleTimerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        }

        /// <summary>
        /// Determines if/how a broadcast packet affects this timer.
        /// </summary>
        private void OnPacketReceived(EntityUid uid, ShuttleTimerComponent component, DeviceNetworkPacketEvent args)
        {
            // currently, all packets are broadcast, and subnetting is implemented by filtering events per-map
            // if i can find a way to *neatly* subnet the timers per-grid, without having the prototypes filter,
            // and pass the frequency to systems, this gets simpler and faster

            var timerXform = Transform(uid);

            // no false positives.
            if (timerXform.MapUid == null)
                return;

            string key;
            string text;
            args.Data.TryGetValue("ShuttleMap", out EntityUid? shuttleMap);
            args.Data.TryGetValue("SourceMap", out EntityUid? source);
            args.Data.TryGetValue("DestMap", out EntityUid? dest);

            switch (timerXform.MapUid)
            {
                case var local when local == shuttleMap:
                    key = "LocalTimer";
                    args.Data.TryGetValue("Docked", out bool docked);
                    text = docked ? "ETD" : "ETA";
                    break;
                case var origin when origin == source:
                    key = "SourceTimer";
                    text = "ETA";
                    break;
                case var remote when remote == dest:
                    key = "DestTimer";
                    args.Data.TryGetValue("Docked", out bool docked_);
                    text = docked_ ? "ETD" : "ETA";
                    break;
                default:
                    return;
            }

            if (!args.Data.TryGetValue(key, out TimeSpan duration))
                return;

            var time = new TextScreenTimerEvent(duration);
            RaiseLocalEvent(uid, ref time);

            var label = new TextScreenTextEvent(new string[] { text });
            RaiseLocalEvent(uid, ref label);
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

        /// <summary>
        /// Helper method for <see cref="EmergencyShuttleSystem"/> and <see cref="RoundEndSystem"/>
        /// </summary>
        /// <param name="duration">Displayed on each evac shuttle timer, in seconds.</param>
        // public void FloodEvacPacket(EntityUid? shuttleMap, EntityUid? sourceMap, EntityUid? destMap,
        //     TimeSpan? localTimer, TimeSpan? sourceTimer, TimeSpan? destTimer)
        // {
        //     var payload = new NetworkPayload
        //     {
        //         ["BroadcastTime"] = duration
        //     };

        //     AllEntityQuery<StationEmergencyShuttleComponent>().MoveNext(out var station, out var comp);
        //     if (comp == null || comp.EmergencyShuttle == null ||
        //         !HasComp<ShuttleTimerComponent>(comp.EmergencyShuttle.Value) ||
        //         !TryComp<DeviceNetworkComponent>(comp.EmergencyShuttle.Value, out var netComp))
        //         return;

        //     _deviceNetworkSystem.QueuePacket(comp.EmergencyShuttle.Value, null, payload, netComp.TransmitFrequency);
        //     return;
        // }
    }
}
