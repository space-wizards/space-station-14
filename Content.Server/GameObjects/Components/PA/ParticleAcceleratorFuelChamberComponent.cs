using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleAcceleratorFuelChamberComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorFuelChamber";

        public override void Initialize()
        {
            base.Initialize();
            ParticleAccelerator.FuelChamber = this;
        }
    }
}
