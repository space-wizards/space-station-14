using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <summary>
/// Returns true if entity is on a grid or in range of one.
/// </summary>
public sealed partial class GridInRangeConditionSystem : EntityConditionSystem<TransformComponent, GridInRangeCondition>
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _map = default!;

    protected override void Condition(Entity<TransformComponent> entity, ref EntityConditionEvent<GridInRangeCondition> args)
    {
        if (entity.Comp.GridUid != null)
        {
            args.Result = true;
            return;
        }

        var worldPos = _transform.GetWorldPosition(entity.Comp);
        var gridRange = new Vector2(args.Condition.Range, args.Condition.Range);

        List<Entity<MapGridComponent>> grids = [];

        _map.FindGridsIntersecting(entity.Comp.MapID, new Box2(worldPos - gridRange, worldPos + gridRange), ref grids);

        args.Result = grids.Count > 0;
    }
}


/// <inheritdoc cref="EntityCondition"/>
public sealed partial class GridInRangeCondition : EntityConditionBase<GridInRangeCondition>
{
    [DataField]
    public float Range = 10f;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
