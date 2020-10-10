using System.Collections.Generic;
using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleAcceleratorPowerBoxComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorPowerBox";
        public PowerConsumerComponent? _powerConsumerComponent;

        public override void Initialize()
        {
            base.Initialize();
            Owner.TryGetComponent(out _powerConsumerComponent);
        }

        public override ParticleAcceleratorPartComponent[] GetNeighbours()
        {
            return new ParticleAcceleratorPartComponent[] {ParticleAccelerator?.EmitterCenter, ParticleAccelerator?.FuelChamber};
        }

        protected override void RegisterAtParticleAccelerator()
        {
            if(ParticleAccelerator == null)
            {
                Logger.Error($"RegisterAtParticleAccelerator called for {this} without connected ParticleAccelerator");
                return;
            }
            ParticleAccelerator.PowerBox = this;
        }

        protected override void UnRegisterAtParticleAccelerator()
        {
            if(ParticleAccelerator == null)
            {
                Logger.Error($"UnRegisterAtParticleAccelerator called for {this} without connected ParticleAccelerator");
                return;
            }
            ParticleAccelerator.PowerBox = null;
        }
    }
}
