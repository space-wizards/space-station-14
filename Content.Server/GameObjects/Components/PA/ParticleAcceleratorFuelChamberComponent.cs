using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleAcceleratorFuelChamberComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorFuelChamber";

        public override ParticleAcceleratorPartComponent[] GetNeighbours()
        {
            return new ParticleAcceleratorPartComponent[] {ParticleAccelerator.ControlBox, ParticleAccelerator.EndCap, ParticleAccelerator.PowerBox};
        }

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
