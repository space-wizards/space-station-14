using Content.Server.DeviceNetwork.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Power.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using System.Diagnostics.CodeAnalysis;
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
            IEntity sender = EntityManager.GetEntity(args.Sender);
            IEntity receiver = EntityManager.GetEntity(uid);

            if (IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(receiver).GridID != IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(sender).GridID)
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
