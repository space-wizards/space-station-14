using Content.Server.Atmos;
using Content.Shared.Atmos;
using Content.Shared.WoodBurner;
using Content.Server.Atmos.EntitySystems;

namespace Content.Server.WoodBurner
{
    [RegisterComponent]
    public sealed class WoodBurnerComponent : SharedWoodBurnerComponent
    {
        //[Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        /*
        public void ReleaseGas()
        {
            var merger = new GasMixture(1) { Temperature = outputGasTemperature };
            merger.SetMoles(Gas.CarbonDioxide, outputGasAmount);
            // InletName
            _atmosphereSystem.Merge(environment, merger);
        }
        */
    }
}
