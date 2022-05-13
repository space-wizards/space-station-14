using Content.Server.DeviceNetwork.Components;
using JetBrains.Annotations;

namespace Content.Server.DeviceNetwork.Systems
{
    [UsedImplicitly]
    public sealed class WirelessNetworkSystem : EntitySystem
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
            var ownPosition = args.SenderPosition;
            var xform = Transform(uid);

            if (xform.MapID != args.SenderTransform.MapID
                || !TryComp<WirelessNetworkComponent?>(args.Sender, out var sendingComponent)
                || (ownPosition - xform.WorldPosition).Length > sendingComponent.Range)
            {
                args.Cancel();
            }
        }
    }
}
