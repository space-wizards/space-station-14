using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Random;

public sealed class RulesSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;
    [Dependency] private readonly AccessReaderSystem _reader = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public bool IsTrue(EntityUid uid, RulesPrototype rules)
    {
        foreach (var rule in rules.Rules)
        {
            switch (rule)
            {
                case AlwaysTrueRule:
                    TransformComponent? xform; // Can't use curly braces for scope because that's linted against.
                    EntityQuery<TransformComponent> xformQuery;
                    bool found;
                    Vector2 worldPos;
                    int count;
                    break;
                case GridInRangeRule griddy:
                    if (!TryComp(uid, out xform))
                    {
                        return false;
                    }

                    if (xform.GridUid != null)
                    {
                        return !griddy.Inverted;
                    }

                    worldPos = _transform.GetWorldPosition(xform);

                    foreach (var _ in _mapManager.FindGridsIntersecting(
                        xform.MapID,
                        new Box2(worldPos - griddy.Range, worldPos + griddy.Range)))
                    {
                        return !griddy.Inverted;
                    }

                    break;
                case InSpaceRule:
                    if (!TryComp(uid, out xform) ||
                        xform.GridUid != null)
                    {
                        return false;
                    }

                    break;
                case NearbyAccessRule access:
                    xformQuery = GetEntityQuery<TransformComponent>();

                    if (!xformQuery.TryGetComponent(uid, out xform) ||
                        xform.MapUid == null)
                    {
                        return false;
                    }

                    found = false;
                    worldPos = _transform.GetWorldPosition(xform, xformQuery);
                    count = 0;

                    // TODO: Update this when we get the callback version
                    foreach (var comp in _lookup.GetComponentsInRange<AccessReaderComponent>(
                        xform.MapID,
                        worldPos,
                        access.Range
                    ))
                    {
                        if (!_reader.AreAccessTagsAllowed(access.Access, comp)
                        || access.Anchored
                        && (!xformQuery.TryGetComponent(comp.Owner, out var compXform) || !compXform.Anchored))
                            continue;

                        count++;

                        if (count < access.Count)
                            continue;

                        found = true;
                        break;
                    }

                    if (!found)
                        return false;

                    break;
                case NearbyComponentsRule nearbyComps:
                    xformQuery = GetEntityQuery<TransformComponent>();

                    if (!xformQuery.TryGetComponent(uid, out xform) ||
                        xform.MapUid == null)
                    {
                        return false;
                    }

                    found = false;
                    worldPos = _transform.GetWorldPosition(xform);
                    count = 0;

                    foreach (var compType in nearbyComps.Components.Values)
                    {
                        // TODO: Update this when we get the callback version
                        foreach (var comp in _lookup.GetComponentsInRange(
                            compType.Component.GetType(),
                            xform.MapID,
                            worldPos,
                            nearbyComps.Range
                        ))
                        {
                            if (nearbyComps.Anchored
                            && (!xformQuery.TryGetComponent(comp.Owner, out var compXform) || !compXform.Anchored))
                                continue;

                            count++;

                            if (count < nearbyComps.Count)
                                continue;

                            found = true;
                            break;
                        }

                        if (found)
                            break;
                    }

                    if (!found)
                        return false;

                    break;
                case NearbyEntitiesRule entity:
                    if (!TryComp<TransformComponent>(uid, out xform) ||
                        xform.MapUid == null)
                    {
                        return false;
                    }

                    found = false;
                    worldPos = _transform.GetWorldPosition(xform);
                    count = 0;

                    foreach (var ent in _lookup.GetEntitiesInRange(xform.MapID, worldPos, entity.Range))
                    {
                        if (!entity.Whitelist.IsValid(ent, EntityManager))
                            continue;

                        count++;

                        if (count < entity.Count)
                            continue;

                        found = true;
                        break;
                    }

                    if (!found)
                        return false;

                    break;
                case NearbyTilesPercentRule tiles:
                    if (!TryComp(uid, out xform) ||
                        !TryComp<MapGridComponent>(xform.GridUid, out var grid))
                    {
                        return false;
                    }

                    var physicsQuery = GetEntityQuery<PhysicsComponent>();
                    var tileCount = 0;
                    var matchingTileCount = 0;

                    foreach (var tile in grid.GetTilesIntersecting(new Circle(_transform.GetWorldPosition(xform), tiles.Range)))
                    {
                        // Only consider collidable anchored (for reasons some subfloor stuff has physics but non-collidable)
                        if (tiles.IgnoreAnchored)
                        {
                            var gridEnum = grid.GetAnchoredEntitiesEnumerator(tile.GridIndices);
                            found = false;

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

                        if (!tiles.Tiles.Contains(_tileDef[tile.Tile.TypeId].ID))
                            continue;

                        matchingTileCount++;
                    }

                    if (tileCount == 0 || matchingTileCount / (float) tileCount < tiles.Percent)
                        return false;

                    break;
                case OnMapGridRule:
                    if (!TryComp(uid, out xform) ||
                        xform.GridUid != xform.MapUid ||
                        xform.MapUid == null)
                    {
                        return false;
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        return true;
    }
}
