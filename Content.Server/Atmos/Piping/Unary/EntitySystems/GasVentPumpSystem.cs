using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Visuals;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Tools.Systems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasVentPumpSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly DeviceNetworkSystem _deviceNetSystem = default!;
        [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
        [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly WeldableSystem _weldable = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasVentPumpComponent, AtmosDeviceUpdateEvent>(OnGasVentPumpUpdated);
            SubscribeLocalEvent<GasVentPumpComponent, AtmosDeviceDisabledEvent>(OnGasVentPumpLeaveAtmosphere);
            SubscribeLocalEvent<GasVentPumpComponent, AtmosDeviceEnabledEvent>(OnGasVentPumpEnterAtmosphere);
            SubscribeLocalEvent<GasVentPumpComponent, AtmosAlarmEvent>(OnAtmosAlarm);
            SubscribeLocalEvent<GasVentPumpComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<GasVentPumpComponent, DeviceNetworkPacketEvent>(OnPacketRecv);
            SubscribeLocalEvent<GasVentPumpComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<GasVentPumpComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<GasVentPumpComponent, SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<GasVentPumpComponent, GasAnalyzerScanEvent>(OnAnalyzed);
            SubscribeLocalEvent<GasVentPumpComponent, WeldableChangedEvent>(OnWeldChanged);
        }

        private void OnGasVentPumpUpdated(EntityUid uid, GasVentPumpComponent vent, ref AtmosDeviceUpdateEvent args)
        {
            //Bingo waz here
            if (_weldable.IsWelded(uid))
            {
                return;
            }

            var nodeName = vent.PumpDirection switch
            {
                VentPumpDirection.Releasing => vent.Inlet,
                VentPumpDirection.Siphoning => vent.Outlet,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (!vent.Enabled
                || !TryComp(uid, out AtmosDeviceComponent? device)
                || !TryComp(uid, out NodeContainerComponent? nodeContainer)
                || !_nodeContainer.TryGetNode(nodeContainer, nodeName, out PipeNode? pipe))
            {
                return;
            }

            var environment = _atmosphereSystem.GetContainingMixture(uid, true, true);

            // We're in an air-blocked tile... Do nothing.
            if (environment == null)
            {
                return;
            }

            var timeDelta =  args.dt;
            var pressureDelta = timeDelta * vent.TargetPressureChange;

            if (vent.PumpDirection == VentPumpDirection.Releasing && pipe.Air.Pressure > 0)
            {
                if (environment.Pressure > vent.MaxPressure)
                    return;

                vent.UnderPressureLockout = (environment.Pressure < vent.UnderPressureLockoutThreshold);

                if ((vent.PressureChecks & VentPressureBound.ExternalBound) != 0)
                {
                    // Vents cannot supply high pressures from an almost empty pipe, instead it's proportional to the pipe
                    //   pressure, up to a limit.
                    // This also means supply pipe pressure indicates minimum pressure on the station, with lower pressure
                    //   sections getting air first.
                    var supplyPressure = MathF.Min(pipe.Air.Pressure * vent.PumpPower, vent.ExternalPressureBound);
                    // Calculate the ratio of supply pressure to current pressure.
                    pressureDelta = MathF.Min(pressureDelta, supplyPressure - environment.Pressure);
                }

                if (pressureDelta <= 0)
                    return;

                // how many moles to transfer to change external pressure by pressureDelta
                // (ignoring temperature differences because I am lazy)
                var transferMoles = pressureDelta * environment.Volume / (pipe.Air.Temperature * Atmospherics.R);

                if (vent.UnderPressureLockout)
                {
                    // Leak only a small amount of gas as a proportion of supply pipe pressure.
                    var pipeDelta = pipe.Air.Pressure - environment.Pressure;
                    transferMoles = (float)timeDelta * pipeDelta * vent.UnderPressureLockoutLeaking;
                    if (transferMoles < 0.0)
                        return;
                }

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

        private void OnGasVentPumpLeaveAtmosphere(EntityUid uid, GasVentPumpComponent component, ref AtmosDeviceDisabledEvent args)
        {
            UpdateState(uid, component);
        }

        private void OnGasVentPumpEnterAtmosphere(EntityUid uid, GasVentPumpComponent component, ref AtmosDeviceEnabledEvent args)
        {
            UpdateState(uid, component);
        }

        private void OnAtmosAlarm(EntityUid uid, GasVentPumpComponent component, AtmosAlarmEvent args)
        {
            if (args.AlarmType == AtmosAlarmType.Danger)
            {
                component.Enabled = false;
            }
            else if (args.AlarmType == AtmosAlarmType.Normal)
            {
                component.Enabled = true;
            }

            UpdateState(uid, component);
        }

        private void OnPowerChanged(EntityUid uid, GasVentPumpComponent component, ref PowerChangedEvent args)
        {
            component.Enabled = args.Powered;
            UpdateState(uid, component);
        }

        private void OnPacketRecv(EntityUid uid, GasVentPumpComponent component, DeviceNetworkPacketEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out DeviceNetworkComponent? netConn)
                || !args.Data.TryGetValue(DeviceNetworkConstants.Command, out var cmd))
                return;

            var payload = new NetworkPayload();

            switch (cmd)
            {
                case AtmosDeviceNetworkSystem.SyncData:
                    payload.Add(DeviceNetworkConstants.Command, AtmosDeviceNetworkSystem.SyncData);
                    payload.Add(AtmosDeviceNetworkSystem.SyncData, component.ToAirAlarmData());

                    _deviceNetSystem.QueuePacket(uid, args.SenderAddress, payload, device: netConn);

                    return;
                case DeviceNetworkConstants.CmdSetState:
                    if (!args.Data.TryGetValue(DeviceNetworkConstants.CmdSetState, out GasVentPumpData? setData))
                        break;

                    component.FromAirAlarmData(setData);
                    UpdateState(uid, component);

                    return;
            }
        }

        private void OnInit(EntityUid uid, GasVentPumpComponent component, ComponentInit args)
        {
            if (component.CanLink)
                _signalSystem.EnsureSinkPorts(uid, component.PressurizePort, component.DepressurizePort);
        }

        private void OnSignalReceived(EntityUid uid, GasVentPumpComponent component, ref SignalReceivedEvent args)
        {
            if (!component.CanLink)
                return;

            if (args.Port == component.PressurizePort)
            {
                component.PumpDirection = VentPumpDirection.Releasing;
                component.ExternalPressureBound = component.PressurizePressure;
                component.PressureChecks = VentPressureBound.ExternalBound;
                UpdateState(uid, component);
            }
            else if (args.Port == component.DepressurizePort)
            {
                component.PumpDirection = VentPumpDirection.Siphoning;
                component.ExternalPressureBound = component.DepressurizePressure;
                component.PressureChecks = VentPressureBound.ExternalBound;
                UpdateState(uid, component);
            }
        }

        private void UpdateState(EntityUid uid, GasVentPumpComponent vent, AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref appearance, false))
                return;

            _ambientSoundSystem.SetAmbience(uid, true);
            if (_weldable.IsWelded(uid))
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                _appearance.SetData(uid, VentPumpVisuals.State, VentPumpState.Welded, appearance);
            }
            else if (!vent.Enabled)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                _appearance.SetData(uid, VentPumpVisuals.State, VentPumpState.Off, appearance);
            }
            else if (vent.PumpDirection == VentPumpDirection.Releasing)
            {
                _appearance.SetData(uid, VentPumpVisuals.State, VentPumpState.Out, appearance);
            }
            else if (vent.PumpDirection == VentPumpDirection.Siphoning)
            {
                _appearance.SetData(uid, VentPumpVisuals.State, VentPumpState.In, appearance);
            }
        }

        private void OnExamine(EntityUid uid, GasVentPumpComponent component, ExaminedEvent args)
        {
            if (!TryComp<GasVentPumpComponent>(uid, out var pumpComponent))
                return;
            if (args.IsInDetailsRange)
            {
                if (pumpComponent.UnderPressureLockout)
                {
                    args.PushMarkup(Loc.GetString("gas-vent-pump-uvlo"));
                }
            }
        }

        /// <summary>
        /// Returns the gas mixture for the gas analyzer
        /// </summary>
        private void OnAnalyzed(EntityUid uid, GasVentPumpComponent component, GasAnalyzerScanEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            var gasMixDict = new Dictionary<string, GasMixture?>();

            // these are both called pipe, above it switches using this so I duplicated that...?
            var nodeName = component.PumpDirection switch
            {
                VentPumpDirection.Releasing => component.Inlet,
                VentPumpDirection.Siphoning => component.Outlet,
                _ => throw new ArgumentOutOfRangeException()
            };
            if (_nodeContainer.TryGetNode(nodeContainer, nodeName, out PipeNode? pipe))
                gasMixDict.Add(nodeName, pipe.Air);

            args.GasMixtures = gasMixDict;
        }

        private void OnWeldChanged(EntityUid uid, GasVentPumpComponent component, ref WeldableChangedEvent args)
        {
            UpdateState(uid, component);
        }
    }
}
