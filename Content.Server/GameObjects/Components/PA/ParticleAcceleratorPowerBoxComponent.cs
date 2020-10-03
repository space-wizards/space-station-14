using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleAcceleratorPowerBoxComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorPowerBox";

        protected override void RegisterAtParticleAccelerator()
        {
            ParticleAccelerator.PowerBox = this;
        }

        protected override void UnRegisterAtParticleAccelerator()
        {
            ParticleAccelerator.PowerBox = null;
        }
    }
}
