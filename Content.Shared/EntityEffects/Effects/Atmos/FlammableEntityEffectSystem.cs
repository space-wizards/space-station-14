using Content.Shared.Atmos.Components;

namespace Content.Shared.EntityEffects.Effects.Atmos;

public abstract partial class SharedFlammableEntityEffectSystem : EntityEffectSystem<FlammableComponent, Flammable>
{
    protected override void Effect(Entity<FlammableComponent> entity, ref EntityEffectEvent<Flammable> args)
    {
        // Server side effect
    }
}

[DataDefinition]
public sealed partial class Flammable : EntityEffectBase<Flammable>
{
    [DataField]
    public float Multiplier = 0.05f;

    // The fire stack multiplier if fire stacks already exist on target, only works if 0 or greater
    [DataField]
    public float MultiplierOnExisting = -1f;

    public override bool ShouldLog => true;
}
