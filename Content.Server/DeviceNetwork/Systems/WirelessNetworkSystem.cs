using Content.Server.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Robust.Shared.Map;
using System.Numerics;
using JetBrains.Annotations;

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
        /// Check if the receiving entity is within a given range of the sending entity's position.
        /// </summary>
        public bool CheckRange(EntityUid receiver, MapId senderMapId, Vector2 senderPosition, int range)
        {            
            var receiverXform = Transform(receiver);

            if (receiverXform.MapID != senderMapId)
                return false;

            return (senderPosition - _transformSystem.GetWorldPosition(receiverXform)).Length() <= range;
        }

        /// <summary>
        /// Gets the position of both the sending and receiving entity and checks if the receiver is in range of the sender.
        /// </summary>
        private void OnBeforePacketSent(EntityUid uid, WirelessNetworkComponent component, BeforePacketSentEvent args)
        {
            // not a wireless to wireless connection, just let it happen
            if (!TryComp<WirelessNetworkComponent>(args.Sender, out var sendingComponent))
                return;

            if (!CheckRange(uid, args.SenderTransform.MapID, args.SenderPosition, sendingComponent.Range))
                args.Cancel();
        }
    }
}