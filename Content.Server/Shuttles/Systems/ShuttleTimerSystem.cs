using Content.Server.Shuttles.Components;
using Content.Server.TextScreen.Events;
using Content.Server.DeviceNetwork.Systems;

// TODO:
// use receivefrequency ids instead of magic numbers
// - emergency shuttle recall inverts timer?
//    i saw this happen once. had to do with removing the activecomponent early
// - deduplicate signaltimer with a maintainer's blessing
// - scan UI?

namespace Content.Server.Shuttles.Systems
{
    /// <summary>
    /// Controls the wallmounted timers on stations and shuttles displaying e.g. FTL duration, ETA
    /// </summary>
    public sealed class ShuttleTimerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShuttleTimerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        }

        /// <summary>
        /// Determines if a broadcast affects this timer, and how.
        /// </summary>
        private void OnPacketReceived(EntityUid uid, ShuttleTimerComponent component, DeviceNetworkPacketEvent args)
        {
            // currently, all packets are broadcast, and subnetting is implemented by filtering events per-map
            // a payload with "BroadcastTime" skips any filtering
            // if i can find a way to *neatly* subnet the timers per-map,
            // and pass the frequency to systems, this gets simpler and faster
            if (args.Data.TryGetValue("BroadcastTime", out TimeSpan broadcast))
            {
                var text = new TextScreenTimerEvent(broadcast);
                RaiseLocalEvent(uid, ref text);
                return;
            }

            var timerXform = Transform(uid);

            // no false positives if mapuid is null
            if (timerXform.MapUid == null)
                return;

            string key;
            args.Data.TryGetValue("ShuttleMap", out EntityUid? shuttleMap);
            args.Data.TryGetValue("SourceMap", out EntityUid? source);
            args.Data.TryGetValue("DestMap", out EntityUid? dest);

            switch (timerXform.MapUid)
            {
                case var local when local == shuttleMap:
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

            var ev = new TextScreenTimerEvent(duration);
            RaiseLocalEvent(uid, ref ev);
        }
    }
}
