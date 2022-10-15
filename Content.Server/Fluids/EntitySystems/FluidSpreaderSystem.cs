using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Fluids.Components;
using Content.Shared.Directions;
using Content.Shared.Doors.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Server.Fluids.EntitySystems;

[UsedImplicitly]
public sealed class FluidSpreaderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;

    /// <summary>
    /// Adds an overflow component to the map data tracking overflowing puddles
    /// </summary>
    /// <param name="puddle"> that's overflowing</param>
    public void AddOverflowingPuddle(PuddleComponent puddle)
    {
        TransformComponent? transformComponent = null;
        if (!Resolve(puddle.Owner, ref transformComponent, false) || transformComponent.MapUid == null)
            return;

        var mapId = transformComponent.MapUid.Value;

        EntityManager.EnsureComponent<FluidMapDataComponent>(mapId, out var component);
        component.Puddles.Add(puddle.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var fluidMapData in EntityQuery<FluidMapDataComponent>())
        {
            if (fluidMapData.Puddles.Count == 0 || _gameTiming.CurTime <= fluidMapData.GoalTime)
                continue;

            var newIteration = new HashSet<EntityUid>();
            foreach (var puddleUid in fluidMapData.Puddles)
            {
                PuddleComponent? puddle = null;
                MetaDataComponent? metaData = null;
                TransformComponent? transform = null;
                if (!Resolve(puddleUid, ref puddle, ref metaData, ref transform, false)
                    || !_mapManager.TryGetGrid(transform.GridUid, out var mapGrid))
                    continue;

                var pos = transform.Coordinates;
                var prototypeName = metaData.EntityPrototype!.ID;
                var puddles = new List<PuddleComponent>(4);
                var totalVolume = _puddleSystem.CurrentVolume(puddle.Owner, puddle);
                foreach (var direction in DirectionRandomizer.RandomCardinal())
                {
                    var newPos = pos.Offset(direction);
                    if (CheckTile(_puddleSystem.CurrentVolume(puddle.Owner, puddle), prototypeName, newPos, mapGrid,
                            out var puddleComponent))
                    {
                        puddles.Add(puddleComponent);
                        totalVolume += _puddleSystem.CurrentVolume(puddleComponent.Owner, puddleComponent);
                    }
                }

                _puddleSystem.EqualizePuddles(puddle.Owner, puddles, totalVolume, newIteration, puddle);
            }

            fluidMapData.Puddles = newIteration;
            fluidMapData.UpdateGoal(_gameTiming.CurTime);
        }
    }


    /// <summary>
    /// Check a tile is valid for solution allocation.
    /// </summary>
    /// <param name="puddleCurrentVolume">parameter used to filter which puddle to spill to.</param>
    /// <param name="prototype">puddle prototype if  a new puddle needs to be constructed</param>
    /// <param name="pos">at which to check tile</param>
    /// <param name="mapGrid">helper param needed to extract entities</param>
    /// <param name="puddle">either found or newly created PuddleComponent.</param>
    /// <returns>true if tile is empty or occupied by a non-overflowing puddle (or a puddle close to being overflowing)</returns>
    private bool CheckTile(FixedPoint2 puddleCurrentVolume, string prototype, EntityCoordinates pos, IMapGrid mapGrid,
        [NotNullWhen(true)] out PuddleComponent? puddle)
    {
        if (!mapGrid.TryGetTileRef(pos, out var tileRef)
            || tileRef.Tile.IsEmpty)
        {
            puddle = null;
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
                puddle = null;
                return false;
            }


            if (!Resolve(entity, ref existingPuddle, false))
            {
                continue;
            }

            // If current puddle has more volume than current we skip that field
            if (_puddleSystem.CurrentVolume(existingPuddle.Owner, existingPuddle) >= puddleCurrentVolume)
            {
                puddle = null;
                return false;
            }

            puddle = existingPuddle;
            return true;
        }

        var puid = EntityManager.SpawnEntity(prototype, pos);
        EntityManager.EnsureComponent<PuddleComponent>(puid, out puddle);
        return true;
    }
}
