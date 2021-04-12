using System;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Unary
{
    [RegisterComponent]
    public class GasPassiveVentComponent : Component, IAtmosProcess
    {
        public override string Name => "GasPassiveVent";

        [DataField("inlet")]
        private string _inletName = "pipe";

        public void ProcessAtmos(float time, IGridAtmosphereComponent atmosphere)
        {
            var environment = atmosphere.GetTile(Owner.Transform.Coordinates)!;

            if (environment.Air == null)
                return;

            if (!Owner.TryGetComponent(out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(_inletName, out PipeNode? inlet))
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
                    inlet.Air.Merge(removed);
                    environment.Invalidate();
                }
            }
        }
    }
}
