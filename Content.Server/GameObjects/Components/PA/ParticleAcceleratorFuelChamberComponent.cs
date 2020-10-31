using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    [ComponentReference(typeof(ParticleAcceleratorPartComponent))]
    public class ParticleAcceleratorFuelChamberComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorFuelChamber";
    }
}
