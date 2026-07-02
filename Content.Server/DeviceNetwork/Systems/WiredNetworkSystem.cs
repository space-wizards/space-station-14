using Content.Server.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using JetBrains.Annotations;

namespace Content.Server.DeviceNetwork.Systems
{
    [UsedImplicitly]
    public sealed class WiredNetworkSystem : BeforeDevicePayloadSystem<WiredNetworkComponent>
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WiredNetworkComponent, BeforePacketSentEvent>(OnBeforePacketSent);
        }

        /// <summary>
        /// Checks if both devices are on the same grid
        /// </summary>
        private void OnBeforePacketSent(Entity<WiredNetworkComponent> ent, ref BeforePacketSentEvent args)
        {
            if (Transform(ent).GridUid != args.SenderTransform.GridUid)
            {
                args.Cancelled = true;
            }
        }

        //Things to do in a future PR:
        //Abstract out the connection between the apcExtensionCable and the apcPowerReceiver
        //Traverse the power cables using path traversal
        //Cache an optimized representation of the traversed path (Probably just cache Devices)

        protected override void OnBeforePayload(Entity<WiredNetworkComponent> ent, ref BeforePacketSentEvent args)
        {
            OnBeforePacketSent(ent, ref args);
        }
    }
}
