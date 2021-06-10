#nullable enable
using Content.Server.Atmos;
using Content.Server.Temperature.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    /// <summary>
    /// Represents that entity can be exposed to Atmos
    /// </summary>
    [RegisterComponent]
    public class AtmosExposedComponent
    : Component
    {
        public override string Name => "AtmosExposed";

        [ViewVariables]
        [ComponentDependency] private readonly TemperatureComponent? _temperatureComponent = null;

        [ViewVariables]
        [ComponentDependency] private readonly BarotraumaComponent? _barotraumaComponent = null;

        [ViewVariables]
        [ComponentDependency] private readonly FlammableComponent? _flammableComponent = null;

        public void Update(TileAtmosphere tile, float frameDelta)
        {
            if (_temperatureComponent != null)
            {
                if (tile.Air != null)
                {
                    var temperatureDelta = tile.Air.Temperature - _temperatureComponent.CurrentTemperature;
                    var heat = temperatureDelta * (tile.Air.HeatCapacity * _temperatureComponent.HeatCapacity / (tile.Air.HeatCapacity + _temperatureComponent.HeatCapacity));
                    _temperatureComponent.ReceiveHeat(heat);
                }
                _temperatureComponent.Update();
            }

            _barotraumaComponent?.Update(tile.Air?.Pressure ?? 0);

            _flammableComponent?.Update(tile);
        }
    }
}
