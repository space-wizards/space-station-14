using Content.Server.DeviceNetwork.Components;
using Content.Server.Station.Systems;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server.DeviceNetwork.Systems
{
    /// <summary>
    /// This system requires the StationLimitedNetworkComponent to be on the the sending entity as well as the receiving entity
    /// </summary>
    [UsedImplicitly]
    public sealed class StationLimitedNetworkSystem : EntitySystem
    {
        [Dependency] private readonly StationSystem _stationSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StationLimitedNetworkComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<StationLimitedNetworkComponent, BeforePacketSentEvent>(OnBeforePacketSent);
        }

        /// <summary>
        /// Sets the station id the device is limited to.
        /// </summary>
        public void SetStation(EntityUid uid, EntityUid? stationId, StationLimitedNetworkComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.StationId = stationId;
        }

        /// <summary>
        /// Set the station id to the one the entity is on when the station limited component is added
        /// </summary>
        private void OnMapInit(EntityUid uid, StationLimitedNetworkComponent networkComponent, MapInitEvent args)
        {
            networkComponent.StationId = _stationSystem.GetOwningStation(uid);
        }

        /// <summary>
        /// Checks if both devices are limited to the same station
        /// </summary>
        private void OnBeforePacketSent(EntityUid uid, StationLimitedNetworkComponent component, BeforePacketSentEvent args)
        {
            if (!CheckStationId(args.Sender, component.AllowNonStationPackets, component.StationId))
            {
                args.Cancel();
            }
        }

        /// <summary>
        /// Compares the station IDs of the sending and receiving network components.
        /// Returns false if either of them doesn't have a station ID or if their station ID isn't equal.
        /// Returns true even when the sending entity isn't tied to a station if `allowNonStationPackets` is set to true.
        /// </summary>
        private bool CheckStationId(EntityUid senderUid, bool allowNonStationPackets, EntityUid? receiverStationId, StationLimitedNetworkComponent? sender = null)
        {
            if (!receiverStationId.HasValue)
                return false;

            if (!Resolve(senderUid, ref sender, false))
                return allowNonStationPackets;

            return sender.StationId == receiverStationId;
        }
    }
}
