using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <summary>
/// Returns true if not intersecting with a wall
/// </summary>
public sealed partial class NotInsideWallConditionSystem : EntityConditionSystem<TransformComponent, NotInsideWallCondition>
{
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    /// <inheritdoc/>
    protected override void Condition(Entity<TransformComponent> entity, ref EntityConditionEvent<NotInsideWallCondition> args)
    {
        var tileref = _turf.GetTileRef(entity.Comp.Coordinates);
        if (tileref != null && _physics.GetEntitiesIntersectingBody(entity, (int)CollisionGroup.Impassable).Count > 0)
        {
            args.Result = false;
            return;
        }
        args.Result = true;
    }
}


/// <inheritdoc cref="EntityCondition"/>
public sealed partial class NotInsideWallCondition : EntityConditionBase<NotInsideWallCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}


