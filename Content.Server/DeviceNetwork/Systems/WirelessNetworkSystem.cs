using Content.Server.DeviceNetwork.Components;
using JetBrains.Annotations;
using Content.Server.DeviceLinking.Events;

namespace Content.Server.DeviceNetwork.Systems
{
    [UsedImplicitly]
    public sealed class WirelessNetworkSystem : EntitySystem
    {
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

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

            // not a wireless to wireless connection, just let it happen
            if (!TryComp<WirelessNetworkComponent>(args.Sender, out var sendingComponent))
                return;

            if (xform.MapID != args.SenderTransform.MapID
                || (ownPosition - _transformSystem.GetWorldPosition(xform)).Length() > sendingComponent.Range)
            {
                var eventArgs = new SignalFailedEvent(args.Sender, true); // event so signallers throw an error if they're out of range
                RaiseLocalEvent(args.Sender, ref eventArgs, false);
                args.Cancel();
            }
            else // so signallers throw a different message on success
            {
                var eventArgs = new SignalFailedEvent(args.Sender, false);
                RaiseLocalEvent(args.Sender, ref eventArgs, false);
            }
        }
    }
}
