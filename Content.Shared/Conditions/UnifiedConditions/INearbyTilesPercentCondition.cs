using Content.Shared.Conditions.Satisfier;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface INearbyTilesPercentCondition : ICondition<INearbyTilesPercentCondition>,
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
public sealed partial class NearbyTilesPercentConditionSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;

    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery;
    [Dependency] private ITileDefinitionManager _tileDef = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<TransformComponent> entity,
        ref ConditionEvaluationEvent<INearbyTilesPercentCondition> args)
    {
        args.Handled = true;

        if (!TryComp<MapGridComponent>(entity.Comp.GridUid, out var grid))
            return;

        var tileCount = 0;
        var matchingTileCount = 0;

        var tiles = _map.GetTilesIntersecting(entity.Comp.GridUid.Value,
            grid,
            new Circle(_transform.GetWorldPosition(entity.Comp), args.Condition.Range));

        foreach (var tile in tiles)
        {
            // Only consider collidable anchored (for reasons some subfloor stuff has physics but non-collidable)
            if (args.Condition.IgnoreAnchored)
            {
                var gridEnum = _map.GetAnchoredEntities(entity.Comp.GridUid.Value, grid, tile.GridIndices);
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

            if (!args.Condition.Tiles.Contains(_tileDef[tile.Tile.TypeId].ID))
                continue;

            matchingTileCount++;
        }

        args.Value = tileCount > 0 ? matchingTileCount / (float)tileCount : 0;
    }
}
