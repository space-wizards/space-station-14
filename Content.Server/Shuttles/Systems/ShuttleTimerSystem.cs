using Content.Server.Shuttles.Components;
using Content.Server.TextScreen.Events;
using Content.Server.DeviceNetwork.Systems;

// TODO:
// - emergency shuttle recall inverts timer?
//    i saw this happen once. had to do with removing the activecomponent early
// - deduplicate signaltimer with a maintainer's blessing
// - scan UI?

namespace Content.Server.Shuttles.Systems
{
    public sealed partial class ShuttleTimerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShuttleTimerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        }

        private void OnPacketReceived(EntityUid uid, ShuttleTimerComponent component, DeviceNetworkPacketEvent args)
        {
            // skip any logic if a packet is broadcasted (again) to all networked timers
            if (args.Data.TryGetValue("BroadcastTime", out float broadcast))
            {
                var text = new TextScreenTimerEvent(TimeSpan.FromSeconds(broadcast));
                RaiseLocalEvent(uid, ref text);
                return;
            }

            var timerXform = Transform(uid);

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

            if (!args.Data.TryGetValue(key, out float duration))
                return;

            var ev = new TextScreenTimerEvent(TimeSpan.FromSeconds(duration));
            RaiseLocalEvent(uid, ref ev);
        }
    }
}
