using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Content.Server.Power.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared.Examine;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasThermoMachineSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly PowerReceiverSystem _power = default!;
        [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
        [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasThermoMachineComponent, AtmosDeviceUpdateEvent>(OnThermoMachineUpdated);
            SubscribeLocalEvent<GasThermoMachineComponent, ExaminedEvent>(OnExamined);

            // UI events
            SubscribeLocalEvent<GasThermoMachineComponent, BeforeActivatableUIOpenEvent>(OnBeforeOpened);
            SubscribeLocalEvent<GasThermoMachineComponent, GasThermomachineToggleMessage>(OnToggleMessage);
            SubscribeLocalEvent<GasThermoMachineComponent, GasThermomachineChangeTemperatureMessage>(OnChangeTemperature);

            // Device network
            SubscribeLocalEvent<GasThermoMachineComponent, DeviceNetworkPacketEvent>(OnPacketRecv);
        }

        private void OnBeforeOpened(Entity<GasThermoMachineComponent> ent, ref BeforeActivatableUIOpenEvent args)
        {
            DirtyUI(ent, ent.Comp);
        }

        private void OnThermoMachineUpdated(EntityUid uid, GasThermoMachineComponent thermoMachine, ref AtmosDeviceUpdateEvent args)
        {
            if (!(_power.IsPowered(uid) && TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
                || !TryComp<NodeContainerComponent>(uid, out var nodeContainer)
                || !_nodeContainer.TryGetNode(nodeContainer, thermoMachine.InletName, out PipeNode? inlet))
            {
                return;
            }

            float sign = Math.Sign(thermoMachine.Cp); // 1 if heater, -1 if freezer
            float targetTemp = thermoMachine.TargetTemperature;
            float highTemp = targetTemp + sign * thermoMachine.TemperatureTolerance;
            float temp = inlet.Air.Temperature;

            if (sign * temp >= sign * highTemp) // upper bound
                thermoMachine.HysteresisState = false; // turn off
            else if (sign * temp < sign * targetTemp) // lower bound
                thermoMachine.HysteresisState = true; // turn on

            if (thermoMachine.HysteresisState)
                targetTemp = highTemp; // when on, target upper hysteresis bound

            if (!thermoMachine.HysteresisState) // Hysteresis is the same as "Should this be on?"
            {
                // Turn dynamic load back on when power has been adjusted to not cause lights to
                // blink every time this heater comes on.
                //receiver.Load = 0f;
                return;
            }

            // Multiply power in by coefficient of performance, add that heat to gas
            float dQ = thermoMachine.HeatCapacity * thermoMachine.Cp * args.dt;

            // Clamps the heat transferred to not overshoot
            float Cin = _atmosphereSystem.GetHeatCapacity(inlet.Air, true);
            float dT = targetTemp - temp;
            float dQLim = dT * Cin;
            float scale = 1f;
            if (Math.Abs(dQ) > Math.Abs(dQLim))
            {
                scale = dQLim / dQ; // reduce power consumption
                thermoMachine.HysteresisState = false; // turn off
            }
            float dQActual = dQ * scale;
            float dQLeak = dQActual * thermoMachine.EnergyLeakPercentage;
            float dQPipe = dQActual - dQLeak;
            _atmosphereSystem.AddHeat(inlet.Air, dQPipe);

            if (_atmosphereSystem.GetContainingMixture(uid) is { } containingMixture)
                _atmosphereSystem.AddHeat(containingMixture, dQLeak);

            receiver.Load = thermoMachine.HeatCapacity;// * scale; // we're not ready for dynamic load yet, see note above
        }

        private bool IsHeater(GasThermoMachineComponent comp)
        {
            return comp.Cp >= 0;
        }

        private void OnToggleMessage(EntityUid uid, GasThermoMachineComponent thermoMachine, GasThermomachineToggleMessage args)
        {
            _power.TogglePower(uid);
            DirtyUI(uid, thermoMachine);
        }

        private void OnChangeTemperature(EntityUid uid, GasThermoMachineComponent thermoMachine, GasThermomachineChangeTemperatureMessage args)
        {
            if (IsHeater(thermoMachine))
                thermoMachine.TargetTemperature = MathF.Min(args.Temperature, thermoMachine.MaxTemperature);
            else
                thermoMachine.TargetTemperature = MathF.Max(args.Temperature, thermoMachine.MinTemperature);
            thermoMachine.TargetTemperature = MathF.Max(thermoMachine.TargetTemperature, Atmospherics.TCMB);
            DirtyUI(uid, thermoMachine);
        }

        private void DirtyUI(EntityUid uid, GasThermoMachineComponent? thermoMachine, UserInterfaceComponent? ui=null)
        {
            if (!Resolve(uid, ref thermoMachine, ref ui, false))
                return;

            ApcPowerReceiverComponent? powerReceiver = null;
            if (!Resolve(uid, ref powerReceiver))
                return;

            _userInterfaceSystem.TrySetUiState(uid, ThermomachineUiKey.Key,
                new GasThermomachineBoundUserInterfaceState(thermoMachine.MinTemperature, thermoMachine.MaxTemperature, thermoMachine.TargetTemperature, !powerReceiver.PowerDisabled, IsHeater(thermoMachine)), null, ui);
        }

        private void OnExamined(EntityUid uid, GasThermoMachineComponent thermoMachine, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            if (Loc.TryGetString("gas-thermomachine-system-examined", out var str,
                        ("machineName", !IsHeater(thermoMachine) ? "freezer" : "heater"),
                        ("tempColor", !IsHeater(thermoMachine) ? "deepskyblue" : "red"),
                        ("temp", Math.Round(thermoMachine.TargetTemperature,2))
               ))

                args.PushMarkup(str);
        }

        private void OnPacketRecv(EntityUid uid, GasThermoMachineComponent component, DeviceNetworkPacketEvent args)
        {
            if (!TryComp(uid, out DeviceNetworkComponent? netConn)
                || !args.Data.TryGetValue(DeviceNetworkConstants.Command, out var cmd))
                return;

            var payload = new NetworkPayload();

            switch (cmd)
            {
                case AtmosDeviceNetworkSystem.SyncData:
                    payload.Add(DeviceNetworkConstants.Command, AtmosDeviceNetworkSystem.SyncData);
                    payload.Add(AtmosDeviceNetworkSystem.SyncData, new GasThermoMachineData(component.LastEnergyDelta));

                    _deviceNetwork.QueuePacket(uid, args.SenderAddress, payload, device: netConn);

                    return;
            }
        }
    }
}
