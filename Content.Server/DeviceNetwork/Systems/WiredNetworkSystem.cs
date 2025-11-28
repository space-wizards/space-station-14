using Content.Server.DeviceNetwork.Components;
using Content.Shared.Destructible;
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
        /// Checks if it can send off station or not
        /// </summary>
        private void OnBeforePacketSent(EntityUid uid, WiredNetworkComponent component, BeforePacketSentEvent args)
        {
            //if connectsOffGrid is true, just let it send the packets

            if (component.ConnectsOffGrid == true)
            {
                return;
            }

            // if they're not on the same grid, cancel 

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
