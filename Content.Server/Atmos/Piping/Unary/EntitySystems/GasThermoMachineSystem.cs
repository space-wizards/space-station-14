using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Construction;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Unary.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasThermoMachineSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasThermoMachineComponent, AtmosDeviceUpdateEvent>(OnThermoMachineUpdated);
            SubscribeLocalEvent<GasThermoMachineComponent, AtmosDeviceDisabledEvent>(OnThermoMachineLeaveAtmosphere);
            SubscribeLocalEvent<GasThermoMachineComponent, RefreshPartsEvent>(OnGasThermoRefreshParts);

            // UI events
            SubscribeLocalEvent<GasThermoMachineComponent, GasThermomachineToggleMessage>(OnToggleMessage);
            SubscribeLocalEvent<GasThermoMachineComponent, GasThermomachineChangeTemperatureMessage>(OnChangeTemperature);
        }

        private void OnThermoMachineUpdated(EntityUid uid, GasThermoMachineComponent thermoMachine, AtmosDeviceUpdateEvent args)
        {
            var appearance = EntityManager.GetComponentOrNull<AppearanceComponent>(thermoMachine.Owner);

            if (!thermoMachine.Enabled
                || !EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
                || !nodeContainer.TryGetNode(thermoMachine.InletName, out PipeNode? inlet))
            {
                DirtyUI(uid, thermoMachine);
                appearance?.SetData(ThermoMachineVisuals.Enabled, false);
                return;
            }

            var airHeatCapacity = _atmosphereSystem.GetHeatCapacity(inlet.Air);
            var combinedHeatCapacity = airHeatCapacity + thermoMachine.HeatCapacity;
            var oldTemperature = inlet.Air.Temperature;

            if (!MathHelper.CloseTo(combinedHeatCapacity, 0, 0.001f))
            {
                appearance?.SetData(ThermoMachineVisuals.Enabled, true);
                var combinedEnergy = thermoMachine.HeatCapacity * thermoMachine.TargetTemperature + airHeatCapacity * inlet.Air.Temperature;
                inlet.Air.Temperature = combinedEnergy / combinedHeatCapacity;
            }

            // TODO ATMOS: Active power usage.
        }

        private void OnThermoMachineLeaveAtmosphere(EntityUid uid, GasThermoMachineComponent component, AtmosDeviceDisabledEvent args)
        {
            if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(ThermoMachineVisuals.Enabled, false);
            }

            DirtyUI(uid, component);
        }

        private void OnGasThermoRefreshParts(EntityUid uid, GasThermoMachineComponent component, RefreshPartsEvent args)
        {
            var matterBinRating = 0;
            var laserRating = 0;

            foreach (var part in args.Parts)
            {
                switch (part.PartType)
                {
                    case MachinePart.MatterBin:
                        matterBinRating += part.Rating;
                        break;
                    case MachinePart.Laser:
                        laserRating += part.Rating;
                        break;
                }
            }

            component.HeatCapacity = 5000 * MathF.Pow((matterBinRating - 1), 2);

            switch (component.Mode)
            {
                // 573.15K with stock parts.
                case ThermoMachineMode.Heater:
                    component.MaxTemperature = Atmospherics.T20C + (component.InitialMaxTemperature * laserRating);
                    break;
                // 73.15K with stock parts.
                case ThermoMachineMode.Freezer:
                    component.MinTemperature = MathF.Max(Atmospherics.T0C - component.InitialMinTemperature + laserRating * 15f, Atmospherics.TCMB);
                    break;
            }

            DirtyUI(uid, component);
        }

        private void OnToggleMessage(EntityUid uid, GasThermoMachineComponent component, GasThermomachineToggleMessage args)
        {
            component.Enabled = !component.Enabled;

            DirtyUI(uid, component);
        }

        private void OnChangeTemperature(EntityUid uid, GasThermoMachineComponent component, GasThermomachineChangeTemperatureMessage args)
        {
            component.TargetTemperature =
                Math.Clamp(args.Temperature, component.MinTemperature, component.MaxTemperature);

            DirtyUI(uid, component);
        }

        private void DirtyUI(EntityUid uid, GasThermoMachineComponent? thermo, ServerUserInterfaceComponent? ui=null)
        {
            if (!Resolve(uid, ref thermo, ref ui, false))
                return;

            _userInterfaceSystem.TrySetUiState(uid, ThermomachineUiKey.Key,
                new GasThermomachineBoundUserInterfaceState(thermo.MinTemperature, thermo.MaxTemperature, thermo.TargetTemperature, thermo.Enabled, thermo.Mode), null, ui);
        }
    }
}
