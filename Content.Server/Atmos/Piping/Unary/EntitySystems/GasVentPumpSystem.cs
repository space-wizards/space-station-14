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
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Visuals;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasVentPumpSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly DeviceNetworkSystem _deviceNetSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasVentPumpComponent, AtmosDeviceUpdateEvent>(OnGasVentPumpUpdated);
            SubscribeLocalEvent<GasVentPumpComponent, AtmosDeviceDisabledEvent>(OnGasVentPumpLeaveAtmosphere);
            SubscribeLocalEvent<GasVentPumpComponent, AtmosMonitorAlarmEvent>(OnAtmosAlarm);
            SubscribeLocalEvent<GasVentPumpComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<GasVentPumpComponent, PacketSentEvent>(OnPacketRecv);
        }

        private void OnGasVentPumpUpdated(EntityUid uid, GasVentPumpComponent vent, AtmosDeviceUpdateEvent args)
        {
            var appearance = EntityManager.GetComponentOrNull<AppearanceComponent>(vent.Owner); //Bingo waz here

            if (vent.Welded)
            {
                appearance?.SetData(VentPumpVisuals.State, VentPumpState.Welded);
                return;
            }

            if (!vent.Enabled
                || !TryComp(uid, out AtmosDeviceComponent? device)
                || !TryComp(uid, out NodeContainerComponent? nodeContainer)
                || !nodeContainer.TryGetNode(vent.InletName, out PipeNode? pipe))
            {
                appearance?.SetData(VentPumpVisuals.State, VentPumpState.Off);
                return;
            }

            var environment = _atmosphereSystem.GetTileMixture(EntityManager.GetComponent<TransformComponent>(vent.Owner).Coordinates, true);

            // We're in an air-blocked tile... Do nothing.
            if (environment == null)
            {
                appearance?.SetData(VentPumpVisuals.State, VentPumpState.Off);
                return;
            }

            var timeDelta =  (_gameTiming.CurTime - device.LastProcess).TotalSeconds;
            var pressureDelta = (float) timeDelta * vent.TargetPressureChange;

            if (vent.PumpDirection == VentPumpDirection.Releasing && pipe.Air.Pressure > 0)
            {
                appearance?.SetData(VentPumpVisuals.State, VentPumpState.Out);

                if (environment.Pressure > vent.MaxPressure)
                    return;

                if ((vent.PressureChecks & VentPressureBound.ExternalBound) != 0)
                    pressureDelta = MathF.Min(pressureDelta, vent.ExternalPressureBound - environment.Pressure);

                if (pressureDelta <= 0)
                    return;

                // how many moles to transfer to change external pressure by pressureDelta
                // (ignoring temperature differences because I am lazy)
                var transferMoles = pressureDelta * environment.Volume / (pipe.Air.Temperature * Atmospherics.R);

                // limit transferMoles so the source doesn't go below its bound.
                if ((vent.PressureChecks & VentPressureBound.InternalBound) != 0)
                {
                    var internalDelta = pipe.Air.Pressure - vent.InternalPressureBound;

                    if (internalDelta <= 0)
                        return;

                    var maxTransfer = internalDelta * pipe.Air.Volume / (pipe.Air.Temperature * Atmospherics.R);
                    transferMoles = MathF.Min(transferMoles, maxTransfer);
                }

                _atmosphereSystem.Merge(environment, pipe.Air.Remove(transferMoles));
            }
            else if (vent.PumpDirection == VentPumpDirection.Siphoning && environment.Pressure > 0)
            {
                appearance?.SetData(VentPumpVisuals.State, VentPumpState.In);

                if (pipe.Air.Pressure > vent.MaxPressure)
                    return;

                if ((vent.PressureChecks & VentPressureBound.InternalBound) != 0)
                    pressureDelta = MathF.Min(pressureDelta, vent.InternalPressureBound - pipe.Air.Pressure);

                if (pressureDelta <= 0)
                    return;

                // how many moles to transfer to change internal pressure by pressureDelta
                // (ignoring temperature differences because I am lazy)
                var transferMoles = pressureDelta * pipe.Air.Volume / (environment.Temperature * Atmospherics.R);

                // limit transferMoles so the source doesn't go below its bound.
                if ((vent.PressureChecks & VentPressureBound.ExternalBound) != 0)
                {
                    var externalDelta = environment.Pressure - vent.ExternalPressureBound;

                    if (externalDelta <= 0)
                        return;

                    var maxTransfer = externalDelta * environment.Volume / (environment.Temperature * Atmospherics.R);

                    transferMoles = MathF.Min(transferMoles, maxTransfer);
                }

                _atmosphereSystem.Merge(pipe.Air, environment.Remove(transferMoles));
            }
        }

        private void OnGasVentPumpLeaveAtmosphere(EntityUid uid, GasVentPumpComponent component, AtmosDeviceDisabledEvent args)
        {
            if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(VentPumpVisuals.State, VentPumpState.Off);
            }
        }

        private void OnAtmosAlarm(EntityUid uid, GasVentPumpComponent component, AtmosMonitorAlarmEvent args)
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

        private void OnPowerChanged(EntityUid uid, GasVentPumpComponent component, PowerChangedEvent args)
        {
            component.Enabled = args.Powered;
        }

        private void OnPacketRecv(EntityUid uid, GasVentPumpComponent component, PacketSentEvent args)
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
                    if (!args.Data.TryGetValue(AirAlarmSystem.AirAlarmSetData, out GasVentPumpData? setData))
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
