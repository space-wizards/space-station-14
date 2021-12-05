using Content.Server.DeviceNetwork.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.DeviceNetwork.Systems
{
    [UsedImplicitly]
    public class WiredNetworkSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WiredNetworkComponent, BeforePacketSentEvent>(OnBeforePacketSent);
        }

        /// <summary>
        /// Checks if both devices are on the same grid
        /// </summary>
        private void OnBeforePacketSent(EntityUid uid, WiredNetworkComponent component, BeforePacketSentEvent args)
        {
            if (IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(uid).GridID != IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(args.Sender).GridID)
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
