using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.NodeContainer;
using Content.Shared.Atmos.Piping;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public class GasThermoMachineSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasThermoMachineComponent, AtmosDeviceUpdateEvent>(OnThermoMachineUpdated);
            SubscribeLocalEvent<GasThermoMachineComponent, AtmosDeviceDisabledEvent>(OnThermoMachineLeaveAtmosphere);
        }

        private void OnThermoMachineUpdated(EntityUid uid, GasThermoMachineComponent thermoMachine, AtmosDeviceUpdateEvent args)
        {
            var appearance = thermoMachine.Owner.GetComponentOrNull<AppearanceComponent>();
            appearance?.SetData(ThermoMachineVisuals.Enabled, false);

            if (!thermoMachine.Enabled)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(thermoMachine.InletName, out PipeNode? inlet))
                return;

            var airHeatCapacity = Get<AtmosphereSystem>().GetHeatCapacity(inlet.Air);
            var combinedHeatCapacity = airHeatCapacity + thermoMachine.HeatCapacity;
            var oldTemperature = inlet.Air.Temperature;

            if (combinedHeatCapacity > 0)
            {
                appearance?.SetData(ThermoMachineVisuals.Enabled, true);
                var combinedEnergy = thermoMachine.HeatCapacity * thermoMachine.TargetTemperature + airHeatCapacity * inlet.Air.Temperature;
                inlet.Air.Temperature = combinedEnergy / combinedHeatCapacity;
            }

            // TODO ATMOS: Active power usage.
        }

        private void OnThermoMachineLeaveAtmosphere(EntityUid uid, GasThermoMachineComponent component, AtmosDeviceDisabledEvent args)
        {
            if (ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(ThermoMachineVisuals.Enabled, false);
            }
        }
    }
}
