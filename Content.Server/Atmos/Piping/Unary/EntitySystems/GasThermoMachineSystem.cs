using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Construction;
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
using Content.Shared.Examine;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasThermoMachineSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly PowerReceiverSystem _power = default!;
        [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
        [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;


        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasThermoMachineComponent, AtmosDeviceUpdateEvent>(OnThermoMachineUpdated);
            SubscribeLocalEvent<GasThermoMachineComponent, RefreshPartsEvent>(OnGasThermoRefreshParts);
            SubscribeLocalEvent<GasThermoMachineComponent, UpgradeExamineEvent>(OnGasThermoUpgradeExamine);
            SubscribeLocalEvent<GasThermoMachineComponent, ExaminedEvent>(OnExamined);

            // UI events
            SubscribeLocalEvent<GasThermoMachineComponent, GasThermomachineToggleMessage>(OnToggleMessage);
            SubscribeLocalEvent<GasThermoMachineComponent, GasThermomachineChangeTemperatureMessage>(OnChangeTemperature);

            // Device network
            SubscribeLocalEvent<GasThermoMachineComponent, DeviceNetworkPacketEvent>(OnPacketRecv);
        }

        private void OnThermoMachineUpdated(EntityUid uid, GasThermoMachineComponent thermoMachine, AtmosDeviceUpdateEvent args)
        {

            if (!(_power.IsPowered(uid) && TryComp(uid, out ApcPowerReceiverComponent? receiver))
                || !TryComp(uid, out NodeContainerComponent? nodeContainer)
                || !_nodeContainer.TryGetNode(nodeContainer, thermoMachine.InletName, out PipeNode? inlet))
            {
                return;
            }

            float sign = Math.Sign(thermoMachine.Cp); // 1 if heater, -1 if freezer

            // Implement hysteresis
            float hystTemp = thermoMachine.TargetTemperature;
            if (thermoMachine.HysteresisState)
            {
                hystTemp += sign*thermoMachine.TemperatureTolerance;
            }

            // Stop if we've reached our target temperature.
            if (sign*inlet.Air.Temperature > sign*hystTemp) // inequality flips if freezer
            {
                thermoMachine.HysteresisState = !thermoMachine.HysteresisState;
                return;
            }

            // Multiply power in by coefficient of performance, add that heat to gas
            float dQ = receiver.Load * thermoMachine.Cp * args.dt;
            _atmosphereSystem.AddHeat(inlet.Air, dQ);

            // If temperature over/undershoots, that's okay! Min/Max temp is about the setpoint.
        }

        private void OnGasThermoRefreshParts(EntityUid uid, GasThermoMachineComponent thermoMachine, RefreshPartsEvent args)
        {
            var heatCapacityPartRating = args.PartRatings[thermoMachine.MachinePartHeatCapacity];
            thermoMachine.HeatCapacity = thermoMachine.BaseHeatCapacity * MathF.Pow(heatCapacityPartRating, 2);
            if (TryComp(uid, out ApcPowerReceiverComponent? receiver))
            {
                receiver.Load = thermoMachine.HeatCapacity;
            }

            var temperatureRangePartRating = args.PartRatings[thermoMachine.MachinePartTemperature];
            switch (thermoMachine.Mode)
            {
                // 593.15K with stock parts.
                case ThermoMachineMode.Heater:
                    thermoMachine.MaxTemperature = thermoMachine.BaseMaxTemperature + thermoMachine.MaxTemperatureDelta * temperatureRangePartRating;
                    thermoMachine.MinTemperature = Atmospherics.T20C;
                    break;
                // 73.15K with stock parts.
                case ThermoMachineMode.Freezer:
                    thermoMachine.MinTemperature = MathF.Max(
                        thermoMachine.BaseMinTemperature - thermoMachine.MinTemperatureDelta * temperatureRangePartRating, Atmospherics.TCMB);
                    thermoMachine.MaxTemperature = Atmospherics.T20C;
                    break;
            }

            DirtyUI(uid, thermoMachine);
        }

        private void OnGasThermoUpgradeExamine(EntityUid uid, GasThermoMachineComponent thermoMachine, UpgradeExamineEvent args)
        {
            switch (thermoMachine.Mode)
            {
                case ThermoMachineMode.Heater:
                    args.AddPercentageUpgrade("gas-thermo-component-upgrade-heating", thermoMachine.MaxTemperature / (thermoMachine.BaseMaxTemperature + thermoMachine.MaxTemperatureDelta));
                    break;
                case ThermoMachineMode.Freezer:
                    args.AddPercentageUpgrade("gas-thermo-component-upgrade-cooling", thermoMachine.MinTemperature / (thermoMachine.BaseMinTemperature - thermoMachine.MinTemperatureDelta));
                    break;
            }
            args.AddPercentageUpgrade("gas-thermo-component-upgrade-heat-capacity", thermoMachine.HeatCapacity / thermoMachine.BaseHeatCapacity);
        }

        private void OnToggleMessage(EntityUid uid, GasThermoMachineComponent thermoMachine, GasThermomachineToggleMessage args)
        {
            _power.TogglePower(uid);
            DirtyUI(uid, thermoMachine);
        }

        private void OnChangeTemperature(EntityUid uid, GasThermoMachineComponent thermoMachine, GasThermomachineChangeTemperatureMessage args)
        {
            thermoMachine.TargetTemperature =
                Math.Clamp(args.Temperature, thermoMachine.MinTemperature, thermoMachine.MaxTemperature);

            DirtyUI(uid, thermoMachine);
        }

        private void DirtyUI(EntityUid uid, GasThermoMachineComponent? thermoMachine, ServerUserInterfaceComponent? ui=null)
        {
            if (!Resolve(uid, ref thermoMachine, ref ui, false))
                return;

            ApcPowerReceiverComponent? powerReceiver = null;
            if (!Resolve(uid, ref powerReceiver))
                return;

            _userInterfaceSystem.TrySetUiState(uid, ThermomachineUiKey.Key,
                new GasThermomachineBoundUserInterfaceState(thermoMachine.MinTemperature, thermoMachine.MaxTemperature, thermoMachine.TargetTemperature, !powerReceiver.PowerDisabled, thermoMachine.Mode), null, ui);
        }

        private void OnExamined(EntityUid uid, GasThermoMachineComponent thermoMachine, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            if (Loc.TryGetString("gas-thermomachine-system-examined", out var str,
                        ("machineName", thermoMachine.Mode == ThermoMachineMode.Freezer ? "freezer" : "heater"),
                        ("tempColor", thermoMachine.Mode == ThermoMachineMode.Freezer ? "deepskyblue" : "red"),
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
