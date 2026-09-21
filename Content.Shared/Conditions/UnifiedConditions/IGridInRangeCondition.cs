using System.Numerics;
using Robust.Shared.Map.Components;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IGridInRangeCondition : ICondition<IGridInRangeCondition>
{
    float Range { get; }
}

/// <summary>
/// Returns true if entity is on a grid or in range of one.
/// </summary>
public sealed partial class GridInRangeConditionSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _map = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<TransformComponent> entity, ref ConditionEvaluationEvent<IGridInRangeCondition> args)
    {
        args.Handled = true;

        if (entity.Comp.GridUid != null)
        {
            args.Value = 1;
            return;
        }

        var worldPos = _transform.GetWorldPosition(entity.Comp);
        var gridRange = new Vector2(args.Condition.Range, args.Condition.Range);

        List<Entity<MapGridComponent>> grids = [];

        _map.FindGridsIntersecting(entity.Comp.MapID, new Box2(worldPos - gridRange, worldPos + gridRange), ref grids);


        args.Value = grids.Count > 0 ? 1 : 0;
    }
}
