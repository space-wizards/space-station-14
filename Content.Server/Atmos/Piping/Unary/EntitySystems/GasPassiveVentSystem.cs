using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasPassiveVentSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasPassiveVentComponent, AtmosDeviceUpdateEvent>(OnPassiveVentUpdated);
        }

        private void OnPassiveVentUpdated(EntityUid uid, GasPassiveVentComponent vent, AtmosDeviceUpdateEvent args)
        {
            var environment = _atmosphereSystem.GetContainingMixture(uid, true, true);

            if (environment == null)
                return;

            if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(vent.InletName, out PipeNode? inlet))
                return;

            var environmentPressure = environment.Pressure;
            var pressureDelta = MathF.Abs(environmentPressure - inlet.Air.Pressure);

            if ((environment.Temperature > 0 || inlet.Air.Temperature > 0) && pressureDelta > 0.5f)
            {
                if (environmentPressure < inlet.Air.Pressure)
                {
                    var airTemperature = environment.Temperature > 0 ? environment.Temperature : inlet.Air.Temperature;
                    var transferMoles = pressureDelta * environment.Volume / (airTemperature * Atmospherics.R);
                    var removed = inlet.Air.Remove(transferMoles);
                    _atmosphereSystem.Merge(environment, removed);
                }
                else
                {
                    var airTemperature = inlet.Air.Temperature > 0 ? inlet.Air.Temperature : environment.Temperature;
                    var outputVolume = inlet.Air.Volume;
                    var transferMoles = (pressureDelta * outputVolume) / (airTemperature * Atmospherics.R);
                    transferMoles = MathF.Min(transferMoles, environment.TotalMoles * inlet.Air.Volume / environment.Volume);
                    var removed = environment.Remove(transferMoles);
                    _atmosphereSystem.Merge(inlet.Air, removed);
                }
            }

        }
    }
}
