using Robust.Shared.Prototypes;

namespace Content.Shared.ParticleAccelerator.Components;

[RegisterComponent]
public sealed partial class ParticleAcceleratorEmitterComponent : Component
{
    [DataField]
    public EntProtoId EmittedPrototype = "ParticlesProjectile";

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
