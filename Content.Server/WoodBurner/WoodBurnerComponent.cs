using Content.Server.Atmos;
using Content.Shared.WoodBurner;
using System;

namespace Content.Server.WoodBurner
{
    [RegisterComponent]
    public class WoodBurnerComponent : SharedWoodBurnerComponent
    {


        public void ReleaseGas()
        {
            //var merger = new GasMixture(1) { Temperature = miner.SpawnTemperature };
            //merger.SetMoles(miner.SpawnGas.Value, miner.SpawnAmount);

            //_atmosphereSystem.Merge(environment, merger);
        }

    }
}
