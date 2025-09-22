using Content.Shared.Eye.Blinding.Systems;

namespace Content.Shared.EntityEffects.Effects.Body;

public sealed partial class EyeDamageEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Template>
{
    [Dependency] private readonly BlindableSystem _blindable = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Template> args)
    {
        _blindable.AdjustEyeDamage(entity.Owner, args.Effect.Amount);
    }
}

public sealed class Template : EntityEffectBase<Template>
{
    /// <summary>
    /// The amount of eye damage we're adding or removing
    /// </summary>
    [DataField]
    public int Amount = -1;
}
