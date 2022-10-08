using System.Linq;
using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Directions;
using Content.Shared.Doors.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Fluids.EntitySystems;

[UsedImplicitly]
public sealed class FluidSpreaderSystem : EntitySystem
{
    /// <summary>
    /// Minimal amount of solution that needs to be transferred.
    /// </summary>
    public static readonly FixedPoint2 MinimalTransfer = FixedPoint2.New(1);

    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;

    /// <summary>
    /// Adds an overflow component to the map data tracking overflowing puddles
    /// </summary>
    /// <param name="puddle"> that's overflowing</param>
    /// <param name="overflownSolution"> extracted from <paramref name="puddle"/> </param>
    public void AddOverflowingPuddle(PuddleComponent puddle, Solution overflownSolution)
    {
        TransformComponent? transformComponent = null;
        if (!Resolve(puddle.Owner, ref transformComponent, false) || transformComponent.MapUid == null)
            return;

        var mapId = transformComponent.MapUid.Value;

        EntityManager.EnsureComponent<FluidMapDataComponent>(mapId, out var component);
        if (!component.FluidSpread.TryGetValue(puddle.Owner, out var overflow))
        {
            var pos = transformComponent.Coordinates.ToVector2i(EntityManager, _mapManager);
            overflow = new OverflowEdgeComponent(overflownSolution)
            {
                ActiveEdge =
                {
                    [pos] = puddle.Owner
                }
            };
            component.FluidSpread[puddle.Owner] = overflow;
        }
        else
        {
            overflow.OverflownSolution.AddSolution(overflownSolution);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var fluidMapData in EntityQuery<FluidMapDataComponent>())
        {
            if (_gameTiming.CurTime <= fluidMapData.GoalTime)
                continue;

            var toRemove = new RemQueue<EntityUid>();
            foreach (var (edgeUid, edge) in fluidMapData.FluidSpread)
            {
                var newPotentialPuddles = new List<PuddlePlacer>(edge.ActiveEdge.Count * 4);
                var visitedTiles = new HashSet<Vector2i>(edge.ActiveEdge.Keys);

                // Go through available edges
                foreach (var (pos, uid) in edge.ActiveEdge)
                {
                    MetaDataComponent? metadataOriginal = null;
                    TransformComponent? transformOrig = null;
                    if (!Resolve(uid, ref metadataOriginal, ref transformOrig, false)
                        || !_mapManager.TryGetGrid(transformOrig.GridUid, out var mapGrid))
                        continue;

                    var prototypeName = metadataOriginal.EntityPrototype!.ID;

                    foreach (var direction in SharedDirectionExtensions.RandomCardinalDirection().ToArray())
                    {
                        var newPos = pos.Offset(direction);
                        if (visitedTiles.Contains(newPos))
                            continue;


                        visitedTiles.Add(newPos);
                        if (!CheckTile(newPos, mapGrid, out var newId))
                            continue;

                        newPotentialPuddles.Add(new PuddlePlacer(newPos, newId, prototypeName,
                            mapGrid.GridTileToLocal(newPos)));
                    }
                }

                var newEdge = SpreadSolutionOnEdge(newPotentialPuddles, edge);
                if (newEdge.Count > 0)
                {
                    edge.ActiveEdge = newEdge;
                }
                else
                {
                    toRemove.Add(edgeUid);
                }

            }

            // Cleanup outside loop to avoid change while iterating error
            foreach (var removeUid in toRemove)
            {
                fluidMapData.FluidSpread.Remove(removeUid);
            }

            fluidMapData.UpdateGoal(_gameTiming.CurTime);
        }
    }

    private Dictionary<Vector2i, EntityUid> SpreadSolutionOnEdge(List<PuddlePlacer> newEdge, OverflowEdgeComponent edge)
    {
        var returnVal = new Dictionary<Vector2i, EntityUid>(newEdge.Count);
        if (newEdge.Count <= 0 || edge.OverflownSolution.CurrentVolume <= MinimalTransfer)
            return returnVal;

        var start = edge.OverflownSolution.CurrentVolume / newEdge.Count;
        foreach (var puddlePlacer in newEdge)
        {
            PuddleComponent? puddle = null;
            if (!Resolve(puddlePlacer.Uid, ref puddle, false))
            {
                puddlePlacer.Uid = EntityManager.SpawnEntity(puddlePlacer.PrototypeName, puddlePlacer.Coordinates);
            }

            var overflow = puddle?.OverflowVolume ?? PuddleComponent.DefaultOverflowVolume;
            var divided = FixedPoint2.Clamp(start, MinimalTransfer, overflow);
            var split = FixedPoint2.Min(divided, _puddleSystem.OverflowLeft(puddlePlacer.Uid, puddle));
            _puddleSystem.TryAddSolution(
                puddlePlacer.Uid,
                edge.OverflownSolution.SplitSolution(split),
                false, false, puddle);

            returnVal[puddlePlacer.Pos] = puddlePlacer.Uid;

            // if solution is spent do not explore
            if (edge.OverflownSolution.CurrentVolume <= FixedPoint2.Zero)
                break;
        }

        return returnVal;
    }

    /// <summary>
    /// Check a tile is valid for solution allocation.
    /// </summary>
    /// <param name="pos">at which to check tile</param>
    /// <param name="mapGrid">helper param needed to extract entities</param>
    /// <param name="newId">either found <c>puddle.Owner</c> or <c>Entity.InvalidUid</c> that denotes that
    /// new puddle will be needed, we do this to prevent double tile puddles.</param>
    /// <returns>true if tile is empty or occupied by a non-overflowing puddle (or a puddle close to being overflowing)</returns>
    private bool CheckTile(Vector2i pos, IMapGrid mapGrid, out EntityUid newId)
    {
        if (!mapGrid.TryGetTileRef(pos, out var tileRef)
            || tileRef.Tile.IsEmpty)
        {
            newId = EntityUid.Invalid;
            return false;
        }

        foreach (var entity in mapGrid.GetAnchoredEntities(pos))
        {
            IPhysBody? physics = null;
            PuddleComponent? existingPuddle = null;

            // This is an invalid location if it is impassable or if it's passable and there are locked door
            if (Resolve(entity, ref physics, false)
                && ((physics.CollisionLayer & (int) CollisionGroup.Impassable) != 0
                    || TryComp<DoorComponent>(entity, out var door) && door.State == DoorState.Closed))
            {
                newId = EntityUid.Invalid;
                return false;
            }

            if (!Resolve(entity, ref existingPuddle, false))
            {
                continue;
            }

            // Do not consider puddles that are overflowing or close to overflowing
            if (_puddleSystem.OverflowLeft(entity, existingPuddle) <= MinimalTransfer)
            {
                newId = EntityUid.Invalid;
                return false;
            }


            newId = entity;
            return true;
        }

        newId = EntityUid.Invalid;
        return true;
    }
}

/// <summary>
/// Helper class for lazy Puddle placement
/// </summary>
public sealed class PuddlePlacer
{
    /// <summary>
    /// Position of puddle
    /// </summary>
    public Vector2i Pos;
    /// <summary>
    /// <c>EntityUid</c> of a Puddle or <c>EntityUid.Invalid</c> if a new puddle needs to be placed
    /// </summary>
    public EntityUid Uid;
    /// <summary>
    /// Prototype of puddle. Needed for constructing new puddle.
    /// </summary>
    public readonly string PrototypeName;
    /// <summary>
    /// Entity coordinate. Needed when placing new puddle.
    /// </summary>
    public EntityCoordinates Coordinates;

    public PuddlePlacer(Vector2i pos, EntityUid uid, string prototypeName, EntityCoordinates coordinates)
    {
        Pos = pos;
        Uid = uid;
        PrototypeName = prototypeName;
        Coordinates = coordinates;
    }
}
