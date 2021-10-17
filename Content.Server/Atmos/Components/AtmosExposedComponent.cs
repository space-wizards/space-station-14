using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Components
{
    // TODO: Kill this. With fire.
    /// <summary>
    /// Represents that entity can be exposed to Atmos
    /// </summary>
    [RegisterComponent]
    public class AtmosExposedComponent : Component
    {
        public override string Name => "AtmosExposed";

        [ViewVariables]
        [ComponentDependency] private readonly TemperatureComponent? _temperatureComponent = null;

        public void Update(GasMixture air, float frameDelta, AtmosphereSystem atmosphereSystem)
        {
            // TODO: I'm coming for you next, TemperatureComponent... Fear me for I am death, destroyer of shitcode.
            if (_temperatureComponent != null)
            {
                var temperatureDelta = air.Temperature - _temperatureComponent.CurrentTemperature;
                var tileHeatCapacity = atmosphereSystem.GetHeatCapacity(air);
                var heat = temperatureDelta * (tileHeatCapacity * _temperatureComponent.HeatCapacity / (tileHeatCapacity + _temperatureComponent.HeatCapacity));
                _temperatureComponent.ReceiveHeat(heat);
                _temperatureComponent.Update();
            }
        }
    }
}
