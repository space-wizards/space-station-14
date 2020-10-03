using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleAcceleratorPowerBoxComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorPowerBox";

        public override void Initialize()
        {
            base.Initialize();
            ParticleAccelerator.PowerBox = this;
        }
    }
}
