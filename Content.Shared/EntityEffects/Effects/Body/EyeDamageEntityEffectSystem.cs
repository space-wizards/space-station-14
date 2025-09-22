using Content.Shared.Eye.Blinding.Systems;

namespace Content.Shared.EntityEffects.Effects.Body;

public sealed partial class EyeDamageEntityEffectSystem : EntityEffectSystem<MetaDataComponent, EyeDamage>
{
    [Dependency] private readonly BlindableSystem _blindable = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<EyeDamage> args)
    {
        _blindable.AdjustEyeDamage(entity.Owner, args.Effect.Amount);
    }
}

[DataDefinition]
public sealed partial class EyeDamage : EntityEffectBase<EyeDamage>
{
    /// <summary>
    /// The amount of eye damage we're adding or removing
    /// </summary>
    [DataField]
    public int Amount = -1;
}
