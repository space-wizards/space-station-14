using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Temperature;
using Robust.Shared.GameObjects;

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

        public void Update(TileAtmosphere tile, float frameDelta)
        {
            if (Owner.TryGetComponent<TemperatureComponent>(out var temperatureComponent))
            {
                if (tile.Air != null)
                {
                    var temperatureDelta = tile.Air.Temperature - temperatureComponent.CurrentTemperature;
                    var heat = temperatureDelta * (tile.Air.HeatCapacity * temperatureComponent.HeatCapacity / (tile.Air.HeatCapacity + temperatureComponent.HeatCapacity));
                    temperatureComponent.ReceiveHeat(heat);
                }
                temperatureComponent.Update();
            }

            if (Owner.TryGetComponent<BarotraumaComponent>(out var barotraumaComponent))
            {
                barotraumaComponent.Update(tile.Air?.Pressure ?? 0);
            }

            if (Owner.TryGetComponent<FlammableComponent>(out var flammableComponent))
            {
                flammableComponent.Update(tile);
            }
        }

    }
}
