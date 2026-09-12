using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Applies or removes cream pie coverage on this entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class SetCreamPiedEntityEffectSystem : EntityEffectSystem<CreamPiedComponent, SetCreamPied>
{
    [Dependency] private SharedCreamPieSystem _creamPie = default!;

    protected override void Effect(Entity<CreamPiedComponent> entity, ref EntityEffectEvent<SetCreamPied> args)
    {
        _creamPie.SetCreamPied(entity.AsNullable(), args.Effect.Enabled);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SetCreamPied : EntityEffectBase<SetCreamPied>
{
    [DataField(required: true)]
    public bool Enabled;
}
