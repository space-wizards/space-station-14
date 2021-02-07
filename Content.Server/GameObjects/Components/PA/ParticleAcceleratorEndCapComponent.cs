using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    [ComponentReference(typeof(ParticleAcceleratorPartComponent))]
    public class ParticleAcceleratorEndCapComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorEndCap";
    }
}
