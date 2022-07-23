using Content.Server.Power.Components;

namespace Content.Server.ParticleAccelerator.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(ParticleAcceleratorPartComponent))]
    public sealed class ParticleAcceleratorPowerBoxComponent : ParticleAcceleratorPartComponent
    {
        [ViewVariables] public PowerConsumerComponent? PowerConsumerComponent;

        protected override void Initialize()
        {
            base.Initialize();

            PowerConsumerComponent = Owner.EnsureComponentWarn<PowerConsumerComponent>();
        }
    }
}
