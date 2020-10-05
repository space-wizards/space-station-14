using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleAcceleratorEndCapComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorEndCap";

        public override ParticleAcceleratorPartComponent[] GetNeighbours()
        {
            return new ParticleAcceleratorPartComponent[] {ParticleAccelerator.FuelChamber};
        }

        protected override void RegisterAtParticleAccelerator()
        {
            ParticleAccelerator.EndCap = this;
        }

        protected override void UnRegisterAtParticleAccelerator()
        {
            ParticleAccelerator.EndCap = null;
        }
    }
}
