using Content.Shared.Singularity.Components;

namespace Content.Server.ParticleAccelerator.Components;

[RegisterComponent]
public sealed partial class ParticleAcceleratorEmitterComponent : Component
{
    [DataField("emittedPrototype")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string EmittedPrototype = "ParticlesProjectile";

    [DataField("emitterType")]
    [ViewVariables(VVAccess.ReadWrite)]
    public ParticleAcceleratorEmitterType Type = ParticleAcceleratorEmitterType.Fore;

    public override string ToString()
    {
        return base.ToString() + $" EmitterType:{Type}";
    }
}

public enum ParticleAcceleratorEmitterType
{
    Port,
    Fore,
    Starboard
}
