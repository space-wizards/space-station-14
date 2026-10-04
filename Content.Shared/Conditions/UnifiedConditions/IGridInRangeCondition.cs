using System.Numerics;
using Robust.Shared.Map.Components;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IGridInRangeCondition : ICondition
{
    float Range { get; }
}

/// <summary>
/// Returns true if entity is on a grid or in range of one.
/// </summary>
public sealed partial class GridInRangeConditionSystem : ConditionEvaluatorSystem<IGridInRangeCondition>
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override float Evaluate(IGridInRangeCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (!TryComp(entityUid, out TransformComponent? transformComponent))
            return 0;

        if (transformComponent.GridUid != null)
        {
            return 1;
        }

        var worldPos = _transform.GetWorldPosition(transformComponent);
        var gridRange = new Vector2(condition.Range, condition.Range);

        List<Entity<MapGridComponent>> grids = [];

        _map.FindGridsIntersecting(transformComponent.MapID, new Box2(worldPos - gridRange, worldPos + gridRange), ref grids);

        return grids.Count > 0 ? 1 : 0;
    }
}
