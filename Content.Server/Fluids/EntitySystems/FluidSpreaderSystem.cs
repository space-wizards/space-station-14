using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Fluids.Components;
using Content.Shared;
using Content.Shared.Directions;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Utility;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Server.Fluids.EntitySystems;

/// <summary>
/// Component that governs overflowing puddles. Controls how Puddles spread and updat
/// </summary>
[UsedImplicitly]
public sealed class FluidSpreaderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;

    /// <summary>
    /// Adds an overflow component to the map data component tracking overflowing puddles
    /// </summary>
    /// <param name="puddleUid">EntityUid of overflowing puddle</param>
    /// <param name="puddle">Optional PuddleComponent</param>
    /// <param name="xform">Optional TransformComponent</param>
    public void AddOverflowingPuddle(EntityUid puddleUid, PuddleComponent? puddle = null,
        TransformComponent? xform = null)
    {
        if (!Resolve(puddleUid, ref puddle, ref xform, false) || xform.MapUid == null)
            return;

        var mapId = xform.MapUid.Value;

        EntityManager.EnsureComponent<FluidMapDataComponent>(mapId, out var component);
        component.Puddles.Add(puddleUid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        Span<Direction> exploreDirections = stackalloc Direction[]
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West,
        };
        var puddles = new List<PuddleComponent>(4);
        var puddleQuery = GetEntityQuery<PuddleComponent>();
        var xFormQuery = GetEntityQuery<TransformComponent>();

        foreach (var fluidMapData in EntityQuery<FluidMapDataComponent>())
        {
            if (fluidMapData.Puddles.Count == 0 || _gameTiming.CurTime <= fluidMapData.GoalTime)
                continue;

            var newIteration = new HashSet<EntityUid>();
            foreach (var puddleUid in fluidMapData.Puddles)
            {
                if (!puddleQuery.TryGetComponent(puddleUid, out var puddle)
                    || !xFormQuery.TryGetComponent(puddleUid, out var transform)
                    || !_mapManager.TryGetGrid(transform.GridUid, out var mapGrid))
                    continue;

                puddles.Clear();
                var pos = transform.Coordinates;

                var totalVolume = _puddleSystem.CurrentVolume(puddle.Owner, puddle);
                exploreDirections.Shuffle();
                foreach (var direction in exploreDirections)
                {
                    var newPos = pos.Offset(direction);
                    if (CheckTile(puddle.Owner, puddle, newPos, mapGrid,
                            out var puddleComponent))
                    {
                        puddles.Add(puddleComponent);
                        totalVolume += _puddleSystem.CurrentVolume(puddleComponent.Owner, puddleComponent);
                    }
                }

                _puddleSystem.EqualizePuddles(puddle.Owner, puddles, totalVolume, newIteration, puddle);
            }

            fluidMapData.Puddles.Clear();
            fluidMapData.Puddles.UnionWith(newIteration);
            fluidMapData.UpdateGoal(_gameTiming.CurTime);
        }
    }


    /// <summary>
    /// Check a tile is valid for solution allocation.
    /// </summary>
    /// <param name="srcUid">Entity Uid of original puddle</param>
    /// <param name="srcPuddle">PuddleComponent attached to srcUid</param>
    /// <param name="pos">at which to check tile</param>
    /// <param name="mapGrid">helper param needed to extract entities</param>
    /// <param name="puddle">either found or newly created PuddleComponent.</param>
    /// <returns>true if tile is empty or occupied by a non-overflowing puddle (or a puddle close to being overflowing)</returns>
    private bool CheckTile(EntityUid srcUid, PuddleComponent srcPuddle, EntityCoordinates pos, MapGridComponent mapGrid,
        [NotNullWhen(true)] out PuddleComponent? puddle)
    {
        if (!mapGrid.TryGetTileRef(pos, out var tileRef)
            || tileRef.Tile.IsEmpty)
        {
            puddle = null;
            return false;
        }

        var puddleCurrentVolume = _puddleSystem.CurrentVolume(srcUid, srcPuddle);

        foreach (var entity in mapGrid.GetAnchoredEntities(pos))
        {
            // If this is valid puddle check if we spread to it.
            if (TryComp(entity, out PuddleComponent? existingPuddle))
            {
                // If current puddle has more volume than current we skip that field
                if (_puddleSystem.CurrentVolume(existingPuddle.Owner, existingPuddle) >= puddleCurrentVolume)
                {
                    puddle = null;
                    return false;
                }

                puddle = existingPuddle;
                return true;
            }

            // if not puddle is this tile blocked by an object like wall or door
            if (TryComp(entity, out PhysicsComponent? physComponent)
                && physComponent.CanCollide
                && (physComponent.CollisionLayer & (int) CollisionGroup.MobMask) != 0)
            {
                puddle = null;
                return false;
            }
        }

        puddle = _puddleSystem.SpawnPuddle(srcUid, pos, srcPuddle);
        return true;
    }
}
