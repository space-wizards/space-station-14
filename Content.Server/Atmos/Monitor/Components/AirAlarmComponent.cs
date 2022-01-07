using System;
using System.Threading;
using System.Collections.Generic;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.DeviceNetwork.Components;
using Content.Server.Power.Components;
using Content.Server.VendingMachines; // TODO: Move this out of vending machines???
using Content.Server.WireHacking;
using Content.Shared.Atmos.Monitor.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;
using static Content.Shared.Wires.SharedWiresComponent;
using static Content.Shared.Wires.SharedWiresComponent.WiresAction;

namespace Content.Server.Atmos.Monitor.Components
{
    [RegisterComponent]
    public class AirAlarmComponent : Component, IWires
    {
        [ComponentDependency] public readonly ApcPowerReceiverComponent? DeviceRecvComponent = default!;
        [ComponentDependency] public readonly AtmosMonitorComponent? AtmosMonitorComponent = default!;
        [ComponentDependency] public readonly DeviceNetworkComponent? DeviceNetComponent = default!;
        [ComponentDependency] public readonly WiresComponent? WiresComponent = null;

        private AirAlarmSystem? _airAlarmSystem;

        [ViewVariables] public AirAlarmMode CurrentMode { get; set; }

        // Remember to null this afterwards.
        [ViewVariables] public IAirAlarmModeUpdate? CurrentModeUpdater { get; set; }

        public override string Name => "AirAlarm";

        public Dictionary<string, IAtmosDeviceData> DeviceData = new();

        public HashSet<NetUserId> ActivePlayers = new();

        public bool FullAccess = false;
        public bool CanSync = true;

        // <-- Wires -->

        private CancellationTokenSource _powerPulsedCancel = new();
        private int PowerPulsedTimeout = 30;

        private enum Wires
        {
            // Cutting this kills power.
            // Pulsing it disrupts power.
            Power,
            // Cutting this allows full access.
            // Pulsing this does nothing.
            Access,
            // Cutting/Remending this resets ONLY from panic mode.
            // Pulsing this sets panic mode.
            Panic,
            // Cutting this clears sync'd devices, and makes
            // the alarm unable to resync.
            // Pulsing this resyncs all devices (ofc current
            // implementation just auto-does this anyways)
            DeviceSync,
            // This does nothing. (placeholder for AI wire,
            // if that ever gets implemented)
            Dummy
        }

        public void RegisterWires(WiresComponent.WiresBuilder builder)
        {
            foreach (var wire in Enum.GetValues<Wires>())
                builder.CreateWire(wire);

            UpdateWires();
        }

        public void UpdateWires()
        {
            if (_airAlarmSystem == null)
                _airAlarmSystem = EntitySystem.Get<AirAlarmSystem>();

            if (WiresComponent == null) return;

            var pwrLightState = (PowerPulsed, PowerCut) switch {
                (true, false) => StatusLightState.BlinkingFast,
                (_, true) => StatusLightState.Off,
                (_, _) => StatusLightState.On
            };

            var powerLight = new StatusLightData(Color.Yellow, pwrLightState, "POWR");

            var accessLight = new StatusLightData(
                Color.Green,
                WiresComponent.IsWireCut(Wires.Access) ? StatusLightState.Off : StatusLightState.On,
                "ACC"
            );

            var panicLight = new StatusLightData(
                Color.Red,
                CurrentMode == AirAlarmMode.Panic ? StatusLightState.On : StatusLightState.Off,
                "PAN"
            );

            var syncLightState = StatusLightState.BlinkingSlow;

            if (AtmosMonitorComponent != null && !AtmosMonitorComponent.NetEnabled)
                syncLightState = StatusLightState.Off;
            else if (DeviceData.Count != 0)
                syncLightState = StatusLightState.On;

            var syncLight = new StatusLightData(Color.Orange, syncLightState, "NET");

            WiresComponent.SetStatus(AirAlarmWireStatus.Power, powerLight);
            WiresComponent.SetStatus(AirAlarmWireStatus.Access, accessLight);
            WiresComponent.SetStatus(AirAlarmWireStatus.Panic, panicLight);
            WiresComponent.SetStatus(AirAlarmWireStatus.DeviceSync, syncLight);
        }

        private bool _powerCut;
        private bool PowerCut
        {
            get => _powerCut;
            set
            {
                _powerCut = value;
                SetPower();
            }
        }

        private bool _powerPulsed;
        private bool PowerPulsed
        {
            get => _powerPulsed && !_powerCut;
            set
            {
                _powerPulsed = value;
                SetPower();
            }
        }

        private void SetPower()
        {
            if (DeviceRecvComponent != null
                && WiresComponent != null)
                DeviceRecvComponent.PowerDisabled = PowerPulsed || PowerCut;
        }

        public void WiresUpdate(WiresUpdateEventArgs args)
        {
            if (DeviceNetComponent == null) return;

            if (_airAlarmSystem == null)
                _airAlarmSystem = EntitySystem.Get<AirAlarmSystem>();

            switch (args.Action)
            {
                case Pulse:
                    switch (args.Identifier)
                    {
                        case Wires.Power:
                            PowerPulsed = true;
                            _powerPulsedCancel.Cancel();
                            _powerPulsedCancel = new CancellationTokenSource();
                            Owner.SpawnTimer(TimeSpan.FromSeconds(PowerPulsedTimeout),
                                () => PowerPulsed = false,
                                _powerPulsedCancel.Token);
                            break;
                        case Wires.Panic:
                            if (CurrentMode != AirAlarmMode.Panic)
                                _airAlarmSystem.SetMode(Owner, DeviceNetComponent.Address, AirAlarmMode.Panic, true, false);
                            break;
                        case Wires.DeviceSync:
                            _airAlarmSystem.SyncAllDevices(Owner);
                            break;
                    }
                    break;
                case Mend:
                    switch (args.Identifier)
                    {
                        case Wires.Power:
                            _powerPulsedCancel.Cancel();
                            PowerPulsed = false;
                            PowerCut = false;
                            break;
                        case Wires.Panic:
                            if (CurrentMode == AirAlarmMode.Panic)
                                _airAlarmSystem.SetMode(Owner, DeviceNetComponent.Address, AirAlarmMode.Filtering, true, false);
                            break;
                        case Wires.Access:
                            FullAccess = false;
                            break;
                        case Wires.DeviceSync:
                            if (AtmosMonitorComponent != null)
                                AtmosMonitorComponent.NetEnabled = true;

                            break;
                    }
                    break;
                case Cut:
                    switch (args.Identifier)
                    {
                        case Wires.DeviceSync:
                            DeviceData.Clear();
                            if (AtmosMonitorComponent != null)
                            {
                                AtmosMonitorComponent.NetworkAlarmStates.Clear();
                                AtmosMonitorComponent.NetEnabled = false;
                            }

                            break;
                        case Wires.Power:
                            PowerCut = true;
                            break;
                        case Wires.Access:
                            FullAccess = true;
                            break;
                    }
                    break;
            }

            UpdateWires();
        }
    }
}
