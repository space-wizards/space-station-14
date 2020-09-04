using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Temperature;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Atmos
{
    /// <summary>
    /// Represents that entity can be exposed to Atmo
    /// </summary>
    [RegisterComponent]
    public class AtmoExposableComponent
    : Component
    {
        public override string Name => "AtmoExposable";

        public void Update(TileAtmosphere tile)
        {
            if(Owner.TryGetComponent<TemperatureComponent>(out var temperatureComponent))
            {
                ProcessTemperature(temperatureComponent, tile);
            }
        }

        private static void ProcessTemperature(TemperatureComponent temperatureComponent, TileAtmosphere tile)
        {
            temperatureComponent.SetTemperature(tile.Air.TemperatureShare(1, temperatureComponent.CurrentTemperature, temperatureComponent.HeatCapacity));
        }
    }
}
