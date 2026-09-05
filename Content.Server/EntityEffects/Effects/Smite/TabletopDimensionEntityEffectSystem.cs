using Content.Server.Tabletop;
using Content.Shared.EntityEffects;
using Content.Shared.Tabletop.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects.Smite;

/// <summary>
/// Spawns a game board and moves this entity into its tabletop session.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class TabletopDimensionEntityEffectSystem : EntityEffectSystem<MetaDataComponent, TabletopDimension>
{
    [Dependency] private TabletopSystem _tabletop = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<TabletopDimension> args)
    {
        var xform = Transform(entity);
        var board = Spawn(args.Effect.Prototype, xform.Coordinates);
        var session = _tabletop.EnsureSession(Comp<TabletopGameComponent>(board));

        _transform.SetMapCoordinates(entity, session.Position);
        _transform.SetWorldRotationNoLerp((entity.Owner, xform), Angle.Zero);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class TabletopDimension : EntityEffectBase<TabletopDimension>
{
    [DataField(required: true)]
    public EntProtoId<TabletopGameComponent> Prototype;
}
