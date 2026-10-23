using Content.Shared.Coordinates.Helpers;

namespace Content.Shared.EntityEffects.Effects.Construction;

/// <summary>
/// Snaps the entity to the grid.
/// </summary>
public sealed partial class SnapToGridEntityEffectSystem : EntityEffectSystem<TransformComponent, SnapToGrid>
{
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SnapToGrid> args)
    {
        if (!entity.Comp.Anchored)
            _transform.SetCoordinates(entity.Owner, entity.Comp, entity.Comp.Coordinates.SnapToGrid(), rotation: args.Effect.SouthRotation ? Angle.Zero : null);
    }
}

public sealed partial class SnapToGrid : EntityEffectBase<SnapToGrid>
{
    /// <summary>
    /// If true the entity will face south when snapped.
    /// </summary>
    [DataField]
    public bool SouthRotation;
}
