using Content.Server.DeviceNetwork.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server.DeviceNetwork.Systems
{
    [UsedImplicitly]
    public sealed class StationLimitedNetworkSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StationLimitedNetworkComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<StationLimitedNetworkComponent, BeforePacketSentEvent>(OnBeforePacketSent);
        }

        /// <summary>
        /// Sets the station id the device is limited to.
        /// Uses the grid id until moonys station beacon system is implemented
        /// </summary>
        public void SetStation(EntityUid uid, GridId stationId, StationLimitedNetworkComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.StationId = stationId;
        }

        /// <summary>
        /// Set the station id to the one the entity is on when the station limited component is added
        /// </summary>
        private void OnComponentInit(EntityUid uid, StationLimitedNetworkComponent networkComponent, ComponentInit args)
        {
            networkComponent.StationId = Transform(uid).GridID;
        }

        /// <summary>
        /// Checks if both devices are limited to the same station
        /// </summary>
        private void OnBeforePacketSent(EntityUid uid, StationLimitedNetworkComponent component, BeforePacketSentEvent args)
        {
            if (!CheckStationId(args.Sender, component.StationId))
            {
                args.Cancel();
            }
        }

        private bool CheckStationId(EntityUid senderUid, GridId receiverStationId, StationLimitedNetworkComponent? sender = null)
        {
            if (!Resolve(senderUid, ref sender))
                return false;

            return sender.StationId == receiverStationId;
        }
    }
}
