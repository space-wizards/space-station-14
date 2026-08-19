using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <summary>
/// Checks if a percentage of the tiles we are nearby match
/// </summary>
public sealed partial class NearbyTilesPercentConditionSystem : EntityConditionSystem<TransformComponent, NearbyTilesPercentCondition>
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private ITileDefinitionManager _tileDef = default!;

    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;

    protected override void Condition(Entity<TransformComponent> entity, ref EntityConditionEvent<NearbyTilesPercentCondition> args)
    {
        if (!TryComp<MapGridComponent>(entity.Comp.GridUid, out var grid))
        {
            args.Result = false;
            return;
        }

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
                var gridEnum = _map.GetAnchoredEntitiesEnumerator(entity.Comp.GridUid.Value, grid, tile.GridIndices);
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

        args.Result = tileCount > 0 && matchingTileCount / (float) tileCount >= args.Condition.Percent;
    }
}


/// <inheritdoc cref="EntityCondition"/>
public sealed partial class NearbyTilesPercentCondition : EntityConditionBase<NearbyTilesPercentCondition>
{
    [DataField]
    public bool IgnoreAnchored;

    [DataField(required: true)]
    public float Percent;

    [DataField(required: true)]
    public List<ProtoId<ContentTileDefinition>> Tiles = new();

    [DataField]
    public float Range = 10f;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
