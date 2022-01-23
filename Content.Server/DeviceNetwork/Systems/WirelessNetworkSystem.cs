using Content.Server.DeviceNetwork.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.DeviceNetwork.Systems
{
    [UsedImplicitly]
    public class WirelessNetworkSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WirelessNetworkComponent, BeforePacketSentEvent>(OnBeforePacketSent);
        }

        /// <summary>
        /// Gets the position of both the sending and receiving entity and checks if the receiver is in range of the sender.
        /// </summary>
        private void OnBeforePacketSent(EntityUid uid, WirelessNetworkComponent component, BeforePacketSentEvent args)
        {
            var ownPosition = EntityManager.GetComponent<TransformComponent>(component.Owner).WorldPosition;
            var position = EntityManager.GetComponent<TransformComponent>(args.Sender).WorldPosition;
            var distance = (ownPosition - position).Length;

            if (EntityManager.TryGetComponent<WirelessNetworkComponent?>(args.Sender, out var sendingComponent) && distance > sendingComponent.Range)
            {
                args.Cancel();
            }
        }
    }
}
