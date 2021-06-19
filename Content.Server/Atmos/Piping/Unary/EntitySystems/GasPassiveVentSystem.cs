using System;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.NodeContainer;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public class GasPassiveVentSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasPassiveVentComponent, AtmosDeviceUpdateEvent>(OnPassiveVentUpdated);
        }

        private void OnPassiveVentUpdated(EntityUid uid, GasPassiveVentComponent vent, AtmosDeviceUpdateEvent args)
        {
            var environment = args.Atmosphere.GetTile(vent.Owner.Transform.Coordinates)!;

            if (environment.Air == null)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(vent.InletName, out PipeNode? inlet))
                return;

            var environmentPressure = environment.Air.Pressure;
            var pressureDelta = MathF.Abs(environmentPressure - inlet.Air.Pressure);

            if ((environment.Air.Temperature > 0 || inlet.Air.Temperature > 0) && pressureDelta > 0.5f)
            {
                if (environmentPressure < inlet.Air.Pressure)
                {
                    var airTemperature = environment.Temperature > 0 ? environment.Temperature : inlet.Air.Temperature;
                    var transferMoles = pressureDelta * environment.Air.Volume / (airTemperature * Atmospherics.R);
                    var removed = inlet.Air.Remove(transferMoles);
                    environment.AssumeAir(removed);
                }
                else
                {
                    var airTemperature = inlet.Air.Temperature > 0 ? inlet.Air.Temperature : environment.Temperature;
                    var outputVolume = inlet.Air.Volume;
                    var transferMoles = (pressureDelta * outputVolume) / (airTemperature * Atmospherics.R);
                    transferMoles = MathF.Min(transferMoles, environment.Air.TotalMoles * inlet.Air.Volume / environment.Air.Volume);
                    var removed = environment.Air.Remove(transferMoles);
                    inlet.AssumeAir(removed);
                    environment.Invalidate();
                }
            }

        }
    }
}
