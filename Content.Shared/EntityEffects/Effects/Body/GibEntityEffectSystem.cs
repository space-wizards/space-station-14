using Content.Shared.Database;
using Content.Shared.Gibbing;

namespace Content.Shared.EntityEffects.Effects.Body;

/// <summary>
/// Gibbs the entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class GibEntityEffectSystem : EntityEffectSystem<TransformComponent, Gib>
{
    [Dependency] private GibbingSystem _gibbing = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<Gib> args)
    {
        _gibbing.Gib(entity, args.Effect.DropGiblets, args.User);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Gib : EntityEffectBase<Gib>
{
    /// <summary>
    /// Whether to drop giblets when gibbing.
    /// </summary>
    [DataField]
    public bool DropGiblets = true;

    public override LogImpact? Impact => LogImpact.Extreme;
}
