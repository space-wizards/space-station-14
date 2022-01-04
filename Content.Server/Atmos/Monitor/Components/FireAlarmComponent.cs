using System;
using System.Threading;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Power.Components;
using Content.Server.VendingMachines; // TODO: Move this out of vending machines???
using Content.Server.WireHacking;
using Content.Shared.Interaction;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using static Content.Shared.Wires.SharedWiresComponent;
using static Content.Shared.Wires.SharedWiresComponent.WiresAction;


namespace Content.Server.Atmos.Monitor.Components
{
    [RegisterComponent]
    public class FireAlarmComponent : Component, IWires
    {
        [ComponentDependency] public readonly ApcPowerReceiverComponent? DeviceRecvComponent = default!;
        [ComponentDependency] public readonly AtmosMonitorComponent? AtmosMonitorComponent = default!;
        [ComponentDependency] public readonly WiresComponent? WiresComponent = null;

        private AtmosMonitorSystem? _atmosMonitorSystem;

        public override string Name => "FireAlarm";

        private CancellationTokenSource _powerPulsedCancel = new();
        private int PowerPulsedTimeout = 30;

        // Much more simpler than the air alarm wire set.
        private enum Wires
        {
            // Cutting this kills power,
            // pulsing it disrupts.
            Power,
            // Cutting this disables network
            // connectivity,
            // pulsing it sets off an alarm.
            Alarm,
            Dummy1,
            Dummy2,
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


        public void RegisterWires(WiresComponent.WiresBuilder builder)
        {
            builder.CreateWire(Wires.Power);
            builder.CreateWire(Wires.Alarm);
            builder.CreateWire(Wires.Dummy1);
            builder.CreateWire(Wires.Dummy2);

            UpdateWires();
        }

        public void UpdateWires()
        {
            if (WiresComponent == null) return;

            var powerLight = new StatusLightData(Color.Yellow, StatusLightState.On, "POWR");

            if (PowerPulsed)
                powerLight = new StatusLightData(Color.Yellow, StatusLightState.BlinkingFast, "POWR");
            else if (PowerCut)
                powerLight = new StatusLightData(Color.Yellow, StatusLightState.Off, "POWR");

            var syncLight = new StatusLightData(Color.Orange, StatusLightState.On, "NET");

            if (AtmosMonitorComponent != null)
                if (!AtmosMonitorComponent.NetEnabled)
                    syncLight = new StatusLightData(Color.Orange, StatusLightState.Off, "NET");
                else if (AtmosMonitorComponent.HighestAlarmInNetwork == AtmosMonitorAlarmType.Danger)
                    syncLight = new StatusLightData(Color.Orange, StatusLightState.BlinkingFast, "NET");

            WiresComponent.SetStatus(FireAlarmWireStatus.Power, powerLight);
            WiresComponent.SetStatus(FireAlarmWireStatus.Alarm, syncLight);
        }

        public void WiresUpdate(WiresUpdateEventArgs args)
        {
            if (_atmosMonitorSystem == null)
                _atmosMonitorSystem = EntitySystem.Get<AtmosMonitorSystem>();

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
                        case Wires.Alarm:
                            if (AtmosMonitorComponent != null)
                                _atmosMonitorSystem.Alert(Owner, AtmosMonitorAlarmType.Danger);
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
                        case Wires.Alarm:
                            if (AtmosMonitorComponent != null)
                                AtmosMonitorComponent.NetEnabled = true;
                            break;
                    }

                    break;
                case Cut:
                    switch (args.Identifier)
                    {
                        case Wires.Power:
                            PowerCut = true;
                            break;
                        case Wires.Alarm:
                            if (AtmosMonitorComponent != null)
                                AtmosMonitorComponent.NetEnabled = false;
                            break;

                    }
                    break;

            }

            UpdateWires();
        }
    }
}
