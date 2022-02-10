using System;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Visuals;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Piping.Unary.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public class GasVentScrubberSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly DeviceNetworkSystem _deviceNetSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasVentScrubberComponent, AtmosDeviceUpdateEvent>(OnVentScrubberUpdated);
            SubscribeLocalEvent<GasVentScrubberComponent, AtmosDeviceDisabledEvent>(OnVentScrubberLeaveAtmosphere);
            SubscribeLocalEvent<GasVentScrubberComponent, AtmosMonitorAlarmEvent>(OnAtmosAlarm);
            SubscribeLocalEvent<GasVentScrubberComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<GasVentScrubberComponent, PacketSentEvent>(OnPacketRecv);

        }

        private void OnVentScrubberUpdated(EntityUid uid, GasVentScrubberComponent scrubber, AtmosDeviceUpdateEvent args)
        {
            var appearance = EntityManager.GetComponentOrNull<AppearanceComponent>(scrubber.Owner);

            if (scrubber.Welded)
            {
                appearance?.SetData(ScrubberVisuals.State, ScrubberState.Welded);
                return;
            }

            if (!scrubber.Enabled
            || !EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
            || !nodeContainer.TryGetNode(scrubber.OutletName, out PipeNode? outlet))
            {
                appearance?.SetData(ScrubberVisuals.State, ScrubberState.Off);
                return;
            }

            var environment = _atmosphereSystem.GetTileMixture(EntityManager.GetComponent<TransformComponent>(scrubber.Owner).Coordinates, true);

            Scrub(_atmosphereSystem, scrubber, appearance, environment, outlet);

            if (!scrubber.WideNet) return;

            // Scrub adjacent tiles too.
            foreach (var adjacent in _atmosphereSystem.GetAdjacentTileMixtures(EntityManager.GetComponent<TransformComponent>(scrubber.Owner).Coordinates, false, true))
            {
                Scrub(_atmosphereSystem, scrubber, null, adjacent, outlet);
            }
        }

        private void OnVentScrubberLeaveAtmosphere(EntityUid uid, GasVentScrubberComponent component, AtmosDeviceDisabledEvent args)
        {
            if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(ScrubberVisuals.State, ScrubberState.Off);
            }
        }

        private void Scrub(AtmosphereSystem atmosphereSystem, GasVentScrubberComponent scrubber, AppearanceComponent? appearance, GasMixture? tile, PipeNode outlet)
        {
            // Cannot scrub if tile is null or air-blocked.
            if (tile == null
                || outlet.Air.Pressure >= 50 * Atmospherics.OneAtmosphere) // Cannot scrub if pressure too high.
            {
                appearance?.SetData(ScrubberVisuals.State, ScrubberState.Off);
                return;
            }

            if (scrubber.PumpDirection == ScrubberPumpDirection.Scrubbing)
            {
                appearance?.SetData(ScrubberVisuals.State, scrubber.WideNet ? ScrubberState.WideScrub : ScrubberState.Scrub);
                var transferMoles = MathF.Min(1f, scrubber.VolumeRate / tile.Volume) * tile.TotalMoles;

                // Take a gas sample.
                var removed = tile.Remove(transferMoles);

                // Nothing left to remove from the tile.
                if (MathHelper.CloseToPercent(removed.TotalMoles, 0f))
                    return;

                atmosphereSystem.ScrubInto(removed, outlet.Air, scrubber.FilterGases);

                // Remix the gases.
                atmosphereSystem.Merge(tile, removed);
            }
            else if (scrubber.PumpDirection == ScrubberPumpDirection.Siphoning)
            {
                appearance?.SetData(ScrubberVisuals.State, ScrubberState.Siphon);
                var transferMoles = tile.TotalMoles * (scrubber.VolumeRate / tile.Volume);

                var removed = tile.Remove(transferMoles);

                _atmosphereSystem.Merge(outlet.Air, removed);
            }
        }

        private void OnAtmosAlarm(EntityUid uid, GasVentScrubberComponent component, AtmosMonitorAlarmEvent args)
        {
            if (args.HighestNetworkType == AtmosMonitorAlarmType.Danger)
            {
                component.Enabled = false;
            }
            else if (args.HighestNetworkType == AtmosMonitorAlarmType.Normal)
            {
                component.Enabled = true;
            }
        }

        private void OnPowerChanged(EntityUid uid, GasVentScrubberComponent component, PowerChangedEvent args) =>
            component.Enabled = args.Powered;

        private void OnPacketRecv(EntityUid uid, GasVentScrubberComponent component, PacketSentEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out DeviceNetworkComponent netConn)
                || !EntityManager.TryGetComponent(uid, out AtmosAlarmableComponent alarmable)
                || !args.Data.TryGetValue(DeviceNetworkConstants.Command, out var cmd))
                return;

            var payload = new NetworkPayload();

            switch (cmd)
            {
                case AirAlarmSystem.AirAlarmSyncCmd:
                    payload.Add(DeviceNetworkConstants.Command, AirAlarmSystem.AirAlarmSyncData);
                    payload.Add(AirAlarmSystem.AirAlarmSyncData, component.ToAirAlarmData());

                    _deviceNetSystem.QueuePacket(uid, args.SenderAddress, AirAlarmSystem.Freq, payload);

                    return;
                case AirAlarmSystem.AirAlarmSetData:
                    if (!args.Data.TryGetValue(AirAlarmSystem.AirAlarmSetData, out GasVentScrubberData? setData))
                        break;

                    component.FromAirAlarmData(setData);
                    alarmable.IgnoreAlarms = setData.IgnoreAlarms;
                    payload.Add(DeviceNetworkConstants.Command, AirAlarmSystem.AirAlarmSetDataStatus);
                    payload.Add(AirAlarmSystem.AirAlarmSetDataStatus, true);

                    _deviceNetSystem.QueuePacket(uid, string.Empty, AirAlarmSystem.Freq, payload, true);

                    return;
            }
        }
    }
}
