using Content.Server.GameObjects.Components.Atmos.Piping;
using Content.Server.GameObjects.Components.Atmos.Piping.Unary;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems.Atmos.Piping.Unary
{
    [UsedImplicitly]
    public class GasThermoMachineSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasThermoMachineComponent, AtmosDeviceUpdateEvent>(OnThermoMachineUpdated);
        }

        private void OnThermoMachineUpdated(EntityUid uid, GasThermoMachineComponent thermoMachine, AtmosDeviceUpdateEvent args)
        {
            if (!thermoMachine.Enabled)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(thermoMachine.InletName, out PipeNode? inlet))
                return;

            var airHeatCapacity = inlet.Air.HeatCapacity;
            var combinedHeatCapacity = airHeatCapacity + thermoMachine.HeatCapacity;
            var oldTemperature = inlet.Air.Temperature;

            if (combinedHeatCapacity > 0)
            {
                var combinedEnergy = thermoMachine.HeatCapacity * thermoMachine.TargetTemperature + airHeatCapacity * inlet.Air.Temperature;
                inlet.Air.Temperature = combinedEnergy / combinedHeatCapacity;
            }

            // TODO ATMOS: Active power usage.
        }
    }
}
