using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleAcceleratorEndCapComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorEndCap";

        public override void Initialize()
        {
            base.Initialize();
            ParticleAccelerator.EndCap = this;
        }
    }
}
