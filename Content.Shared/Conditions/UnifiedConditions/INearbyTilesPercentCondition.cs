using Content.Shared.Conditions.Satisfier;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface INearbyTilesPercentCondition : ICondition,
    IConditionWithDefaultSatisfactionRule
{
    bool IgnoreAnchored { get; }

    float Percent { get; }

    List<ProtoId<ContentTileDefinition>> Tiles { get; }

    float Range { get; }

    Satisfier.Satisfier IConditionWithDefaultSatisfactionRule.GetDefaultSatisfier()
    {
        return new WithThreshold
        {
            Comparison = WithThreshold.Comparator.GreaterEqual,
            Threshold = Percent,
        };
    }
}

/// <summary>
/// Checks if a percentage of the tiles we are nearby match
/// </summary>
public sealed partial class NearbyTilesPercentConditionSystem : ConditionEvaluatorSystem<INearbyTilesPercentCondition>
{
    [Dependency] private SharedMapSystem _map = default!;

    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery;
    [Dependency] private ITileDefinitionManager _tileDef = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override float Evaluate(INearbyTilesPercentCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (!TryComp(entityUid, out TransformComponent? transformComponent))
            return 0;

        if (!TryComp<MapGridComponent>(transformComponent.GridUid, out var grid))
            return 0;

        var tileCount = 0;
        var matchingTileCount = 0;

        var tiles = _map.GetTilesIntersecting(transformComponent.GridUid.Value,
            grid,
            new Circle(_transform.GetWorldPosition(transformComponent), condition.Range));

        foreach (var tile in tiles)
        {
            // Only consider collidable anchored (for reasons some subfloor stuff has physics but non-collidable)
            if (condition.IgnoreAnchored)
            {
                var gridEnum = _map.GetAnchoredEntities(transformComponent.GridUid.Value, grid, tile.GridIndices);
                var found = false;

                while (gridEnum.MoveNext(out var ancUid))
                {
                    if (_physicsQuery.TryGetComponent(ancUid, out var physics) &&
                        physics.CanCollide)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                    continue;
            }

            tileCount++;

            if (!condition.Tiles.Contains(_tileDef[tile.Tile.TypeId].ID))
                continue;

            matchingTileCount++;
        }

        return tileCount > 0 ? matchingTileCount / (float)tileCount : 0;
    }
}
