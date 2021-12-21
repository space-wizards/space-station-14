using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Directions;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.Server.Fluids.EntitySystems;

public partial class PuddleSystem
{
    private readonly List<PuddleExpansion> _puddleExpansionQueue = new();
    private int VisitedEntitiesLimit = 100;

    struct PuddleExpansion
    {
        public List<PuddleComponent> PuddlesToExpand = new();
        public readonly HashSet<Vector2i> VisitedTiles = new();
        public Solution RemainingSolution = new();
    }

    public void AddOverflowingPuddle(PuddleComponent puddleComponent)
    {
        var puddleExpansion = new PuddleExpansion();
        if (_solutionContainerSystem.TryGetSolution(puddleComponent.Owner, puddleComponent.SolutionName,
                out var puddleSolution))
        {
            var removedQuantity = FixedPoint2.Max(puddleSolution.CurrentVolume - PuddleComponent.DefaultOverflowVolume,
                FixedPoint2.Zero);
            puddleExpansion.RemainingSolution = puddleSolution.SplitSolution(removedQuantity);
        }

        puddleExpansion.PuddlesToExpand.Add(puddleComponent);
        _puddleExpansionQueue.Add(puddleExpansion);
    }

    /// <summary>
    ///     Whether adding this solution to this puddle would overflow.
    /// </summary>
    /// <param name="uid">Uid of owning entity</param>
    /// <param name="puddle">Puddle to which we are adding solution</param>
    /// <param name="solution">Solution we intend to add</param>
    /// <returns></returns>
    public bool WouldOverflow(EntityUid uid, Solution solution, PuddleComponent? puddle = null)
    {
        if (!Resolve(uid, ref puddle))
            return false;

        return puddle.CurrentVolume + solution.TotalVolume > puddle.OverflowVolume;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateOverflows();
    }

    private void UpdateOverflows()
    {
        var visitedEntities = 0;
        while (_puddleExpansionQueue.Count > 0)
        {
            var puddleExpansion = _puddleExpansionQueue.Pop();
            while (puddleExpansion.RemainingSolution.CurrentVolume > FixedPoint2.Zero)
            {
                var nextToExpand = new List<Func<PuddleComponent>>();
                foreach (var expand in puddleExpansion.PuddlesToExpand)
                {
                    nextToExpand.AddRange(ExpandPuddles(expand, ref puddleExpansion));
                }

                var max = nextToExpand.Count;

                if (max == 0 || puddleExpansion.RemainingSolution.CurrentVolume / max <= FixedPoint2.Epsilon)
                {
                    // if nowhere to expand we destroy some of solution
                    // less painfull than redistributing it in already filled
                    puddleExpansion.RemainingSolution = new Solution();
                }

                var transferMax = FixedPoint2.Min(PuddleComponent.DefaultOverflowVolume,
                    puddleExpansion.RemainingSolution.CurrentVolume / max);

                puddleExpansion.PuddlesToExpand.Clear();
                foreach (var puddlePlacer in nextToExpand)
                {
                    if (puddleExpansion.RemainingSolution.CurrentVolume <= FixedPoint2.Zero)
                        continue;

                    var puddle = puddlePlacer.Invoke();
                    puddleExpansion.PuddlesToExpand.Add(puddle);
                    visitedEntities++;

                    if (puddle.CurrentVolume >= PuddleComponent.DefaultOverflowVolume)
                        continue;

                    TryAddSolution(puddle.Owner, puddleExpansion.RemainingSolution.SplitSolution(transferMax), false, false,
                        puddle);
                }

                if (visitedEntities < VisitedEntitiesLimit) continue;

                _puddleExpansionQueue.Add(puddleExpansion);
                return;
            }
        }
    }

    /// <summary>
    /// Find or get all 8 direction puddles that have less volume than the expandingPuddle
    /// </summary>
    /// <param name="sourcePuddle"></param>
    /// <param name="puddleExpansion"></param>
    /// <returns></returns>
    private List<Func<PuddleComponent>> ExpandPuddles(PuddleComponent sourcePuddle, ref PuddleExpansion puddleExpansion)
    {
        MetaDataComponent? metadata = null;
        TransformComponent? transform = null;

        if (!Resolve(sourcePuddle.Owner, ref metadata, ref transform, false))
            return new List<Func<PuddleComponent>>();

        var mapGrid = _mapManager.GetGrid(transform.GridID);
        var puddlesToExpand = new List<Func<PuddleComponent>>();
        var tilePos = transform.Coordinates.ToVector2i(EntityManager, _mapManager);
        var prototypeId = metadata.EntityPrototype!.ID;

        puddleExpansion.VisitedTiles.Add(tilePos);
        var directions = SharedDirectionExtensions.RandomDirections().ToArray();
        foreach (var direction in directions)
        {
            var newPos = tilePos.Offset(direction);
            if (puddleExpansion.VisitedTiles.Contains(newPos)) continue;

            puddleExpansion.VisitedTiles.Add(newPos);
            if (TryGetAdjacentPuddle(prototypeId, transform, direction, mapGrid, out var puddle))
            {
                puddlesToExpand.Add(puddle);
            }
        }


        return puddlesToExpand;
    }

    private bool TryGetAdjacentPuddle(string prototype, TransformComponent originPuddle, Direction direction,
        IMapGrid mapGrid, [NotNullWhen(true)] out Func<PuddleComponent>? puddlePlacer)
    {
        var coords = originPuddle.Coordinates;

        if (!coords.Offset(direction).TryGetTileRef(out var tile)
            || tile.Value.Tile.IsEmpty
            || !originPuddle.Anchored)

        {
            puddlePlacer = null;
            return false;
        }

        puddlePlacer = null;
        foreach (var entity in mapGrid.GetAnchoredEntities(coords.Offset(direction)))
        {
            IPhysBody? physics = null;

            // Invalid location skip
            if (Resolve(entity, ref physics, false)
                && (physics.CollisionLayer & (int) CollisionGroup.Impassable) != 0)
            {
                puddlePlacer = default;
                return false;
            }

            // reuse existing puddles, just not ones already visited
            PuddleComponent? existingPuddle = null;

            if (!Resolve(entity, ref existingPuddle, false)) continue;

            puddlePlacer = () => existingPuddle;
        }

        // if none found construct our own
        puddlePlacer ??=  () =>
        {
            var uid = EntityManager.SpawnEntity(prototype,
                mapGrid.DirectionToGrid(originPuddle.Coordinates, direction));
            return EntityManager.GetComponent<PuddleComponent>(uid);
        };
        return true;
    }
}
