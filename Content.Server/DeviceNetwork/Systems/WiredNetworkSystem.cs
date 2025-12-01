using Content.Server.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using JetBrains.Annotations;

namespace Content.Server.DeviceNetwork.Systems
{
    [UsedImplicitly]
    public sealed class WiredNetworkSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WiredNetworkComponent, BeforePacketSentEvent>(OnBeforePacketSent);
        }

        /// <summary>
        /// Handles wired network logic, allowing or denying connectivity
        /// </summary>
        private void OnBeforePacketSent(EntityUid uid, WiredNetworkComponent component, BeforePacketSentEvent args)
        {
            // If the entity can connect off grid, let it send the packets
            if (component.ConnectsOffGrid)
            {
                return;
            }

            // If they're not on the same grid, cancel 
            if (Transform(uid).GridUid != args.SenderTransform.GridUid)
            {
                args.Cancel();
            }
        }

        //Things to do in a future PR:
        //Abstract out the connection between the apcExtensionCable and the apcPowerReceiver
        //Traverse the power cables using path traversal
        //Cache an optimized representation of the traversed path (Probably just cache Devices)
    }
}
