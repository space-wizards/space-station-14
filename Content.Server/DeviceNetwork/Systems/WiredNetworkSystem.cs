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
        /// Handles wired network logic, allowing or denying connectivity.
        /// </summary>
        private void OnBeforePacketSent(EntityUid uid, WiredNetworkComponent component, BeforePacketSentEvent args)
        {
            // If either of the entities transferring packets are the pondering orb, let it send the packets
            if (MetaData(uid).EntityName == "pondering orb" || (MetaData(args.Sender)).EntityName == "pondering orb")
                return;

            // If they're not on the same grid, cancel the packet transfer 
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
