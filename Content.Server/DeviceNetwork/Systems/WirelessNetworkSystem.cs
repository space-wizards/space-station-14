using Content.Server.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using JetBrains.Annotations;

namespace Content.Server.DeviceNetwork.Systems
{
    [UsedImplicitly]
    public sealed partial class WirelessNetworkSystem : BeforeDevicePayloadSystem<WirelessNetworkComponent>
    {
        [Dependency] private SharedTransformSystem _transformSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WirelessNetworkComponent, BeforePacketSentEvent>(OnBeforePacketSent);
        }

        /// <summary>
        /// Gets the position of both the sending and receiving entity and checks if the receiver is in range of the sender.
        /// </summary>
        private void OnBeforePacketSent(Entity<WirelessNetworkComponent> ent, ref BeforePacketSentEvent args)
        {
            var ownPosition = args.SenderPosition;
            var xform = Transform(ent);

            // not a wireless to wireless connection, just let it happen
            if (!TryComp<WirelessNetworkComponent>(args.Sender, out var sendingComponent))
                return;

            if (xform.MapID != args.SenderTransform.MapID
                || (ownPosition - _transformSystem.GetWorldPosition(xform)).Length() > sendingComponent.Range)
            {
                args.Cancelled = true;
            }
        }

        protected override void OnBeforePayload(Entity<WirelessNetworkComponent> ent, ref BeforePacketSentEvent args)
        {
            OnBeforePacketSent(ent, ref args);
        }
    }
}
