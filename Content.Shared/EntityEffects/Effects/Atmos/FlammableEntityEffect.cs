namespace Content.Shared.EntityEffects.Effects.Atmos;

// Server side system

public sealed partial class Flammable : EntityEffectBase<Flammable>
{
    [DataField]
    public float Multiplier = 0.05f;

    // The fire stack multiplier if fire stacks already exist on target, only works if 0 or greater
    [DataField]
    public float MultiplierOnExisting = -1f;

    public override bool ShouldLog => true;
}
