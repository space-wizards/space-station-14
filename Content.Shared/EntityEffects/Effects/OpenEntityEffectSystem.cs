using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Opens the entity, e.g. a drink or food container.
/// If it is already open nothing happens.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class OpenEntityEffectSystem : EntityEffectSystem<OpenableComponent, Open>
{
    [Dependency] private OpenableSystem _openable = default!;

    protected override void Effect(Entity<OpenableComponent> entity, ref EntityEffectEvent<Open> args)
    {
        _openable.TryOpen(entity, entity.Comp, args.User);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Open : EntityEffectBase<Open>;
