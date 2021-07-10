#nullable enable
using Content.Server.Power.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.ParticleAccelerator.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(ParticleAcceleratorPartComponent))]
    public class ParticleAcceleratorPowerBoxComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorPowerBox";
        [ViewVariables] public PowerConsumerComponent? PowerConsumerComponent;

        protected override void Initialize()
        {
            base.Initialize();

            PowerConsumerComponent = Owner.EnsureComponentWarn<PowerConsumerComponent>();
        }
    }
}
