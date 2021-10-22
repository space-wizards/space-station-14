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
            var sender = EntityManager.GetEntity(args.Sender);

            var ownPosition = component.Owner.Transform.WorldPosition;
            var position = sender.Transform.WorldPosition;
            var distance = (ownPosition - position).Length;

            if(sender.TryGetComponent<WirelessNetworkComponent>(out var sendingComponent) && distance > sendingComponent.Range)
            {
                args.Cancel();
            }
        }
    }
}
