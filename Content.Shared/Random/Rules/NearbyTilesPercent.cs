using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Random.Rules;

public sealed partial class NearbyTilesPercentRule : RulesRule
{
    /// <summary>
    /// If there are anchored entities on the tile do we ignore the tile.
    /// </summary>
    [DataField]
    public bool IgnoreAnchored;

    [DataField(required: true)]
    public float Percent;

    [DataField(required: true)]
    public List<ProtoId<ContentTileDefinition>> Tiles = new();

    [DataField]
    public float Range = 10f;

    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        if (!entManager.TryGetComponent(uid, out TransformComponent? xform) ||
            !entManager.TryGetComponent<MapGridComponent>(xform.GridUid, out var grid))
        {
            return false;
        }

        var transform = entManager.System<SharedTransformSystem>();
        var tileDef = IoCManager.Resolve<ITileDefinitionManager>();

        var physicsQuery = entManager.GetEntityQuery<PhysicsComponent>();
        var tileCount = 0;
        var matchingTileCount = 0;

        foreach (var tile in grid.GetTilesIntersecting(new Circle(transform.GetWorldPosition(xform),
                     Range)))
        {
            // Only consider collidable anchored (for reasons some subfloor stuff has physics but non-collidable)
            if (IgnoreAnchored)
            {
                var gridEnum = grid.GetAnchoredEntitiesEnumerator(tile.GridIndices);
                var found = false;

                while (gridEnum.MoveNext(out var ancUid))
                {
                    if (!physicsQuery.TryGetComponent(ancUid, out var physics) ||
                        !physics.CanCollide)
                    {
                        continue;
                    }

                    found = true;
                    break;
                }

                if (found)
                    continue;
            }

            tileCount++;

            if (!Tiles.Contains(tileDef[tile.Tile.TypeId].ID))
                continue;

            matchingTileCount++;
        }

        if (tileCount == 0 || matchingTileCount / (float) tileCount < Percent)
            return Inverted;

        return !Inverted;
    }
}
