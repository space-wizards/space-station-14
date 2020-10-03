using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleAcceleratorFuelChamberComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorFuelChamber";

        protected override void RegisterAtParticleAccelerator()
        {
            ParticleAccelerator.FuelChamber = this;
        }

        protected override void UnRegisterAtParticleAccelerator()
        {
            ParticleAccelerator.FuelChamber = null;
        }
    }
}
