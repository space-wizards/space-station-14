using Content.Server.Power.Components;

namespace Content.Server.ParticleAccelerator.Components;

[RegisterComponent]
public sealed class ParticleAcceleratorPowerBoxComponent : Component
{
    [ViewVariables] public PowerConsumerComponent? PowerConsumerComponent;

    protected override void Initialize()
    {
        base.Initialize();

        PowerConsumerComponent = Owner.EnsureComponentWarn<PowerConsumerComponent>();
    }
}
