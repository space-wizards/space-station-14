using Content.Shared.Singularity.Components;

namespace Content.Server.ParticleAccelerator.Components;

[RegisterComponent]
public sealed class ParticleProjectileComponent : Component
{
    public ParticleAcceleratorPowerState State;
}
