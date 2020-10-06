using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleAcceleratorEndCapComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorEndCap";

        public override ParticleAcceleratorPartComponent[] GetNeighbours()
        {
            return new ParticleAcceleratorPartComponent[] {ParticleAccelerator?.FuelChamber};
        }

        protected override void RegisterAtParticleAccelerator()
        {
            if(ParticleAccelerator == null)
            {
                Logger.Error($"RegisterAtParticleAccelerator called for {this} without connected ParticleAccelerator");
                return;
            }
            ParticleAccelerator.EndCap = this;
        }

        protected override void UnRegisterAtParticleAccelerator()
        {
            if(ParticleAccelerator == null)
            {
                Logger.Error($"UnRegisterAtParticleAccelerator called for {this} without connected ParticleAccelerator");
                return;
            }
            ParticleAccelerator.EndCap = null;
        }
    }
}
