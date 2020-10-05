using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleAcceleratorPowerBoxComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorPowerBox";

        public override ParticleAcceleratorPartComponent[] GetNeighbours()
        {
            return new ParticleAcceleratorPartComponent[] {ParticleAccelerator.EmitterCenter, ParticleAccelerator.FuelChamber};
        }

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
