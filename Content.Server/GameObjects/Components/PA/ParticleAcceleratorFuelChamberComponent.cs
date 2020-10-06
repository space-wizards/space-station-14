using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleAcceleratorFuelChamberComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorFuelChamber";

        public override ParticleAcceleratorPartComponent[] GetNeighbours()
        {
            return new ParticleAcceleratorPartComponent[] {ParticleAccelerator?.ControlBox, ParticleAccelerator?.EndCap, ParticleAccelerator?.PowerBox};
        }

        protected override void RegisterAtParticleAccelerator()
        {
            if(ParticleAccelerator == null)
            {
                Logger.Error($"RegisterAtParticleAccelerator called for {this} without connected ParticleAccelerator");
                return;
            }
            ParticleAccelerator.FuelChamber = this;
        }

        protected override void UnRegisterAtParticleAccelerator()
        {
            if(ParticleAccelerator == null)
            {
                Logger.Error($"UnRegisterAtParticleAccelerator called for {this} without connected ParticleAccelerator");
                return;
            }
            ParticleAccelerator.FuelChamber = null;
        }
    }
}
