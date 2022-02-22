using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Directions;
using Content.Shared.FixedPoint;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.Server.Fluids.EntitySystems;

[UsedImplicitly]
public sealed class FluidSpreaderSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;


    private float _accumulatedTimeFrame;
    private HashSet<EntityUid> _fluidSpread = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<FluidSpreaderComponent, ComponentAdd>((uid, component, _) =>
            FluidSpreaderAdd(uid, component));
    }

    public void AddOverflowingPuddle(PuddleComponent puddleComponent, Solution? solution = null)
    {
        var puddleSolution = solution;
        if (puddleSolution == null && !_solutionContainerSystem.TryGetSolution(puddleComponent.Owner,
                puddleComponent.SolutionName,
                out puddleSolution)) return;

        var spreaderComponent = EntityManager.EnsureComponent<FluidSpreaderComponent>(puddleComponent.Owner);
        spreaderComponent.OverflownSolution = puddleSolution;
        spreaderComponent.Enabled = true;
        FluidSpreaderAdd(spreaderComponent.Owner, spreaderComponent);
    }

    private void FluidSpreaderAdd(EntityUid uid, FluidSpreaderComponent component)
    {
        if (component.Enabled)
            _fluidSpread.Add(uid);
    }

    public override void Update(float frameTime)
    {
        _accumulatedTimeFrame += frameTime;

        if (!(_accumulatedTimeFrame >= 1.0f))
            return;

        _accumulatedTimeFrame -= 1.0f;

        base.Update(frameTime);

        var remQueue = new RemQueue<EntityUid>();
        foreach (var uid in _fluidSpread)
        {
            if (!TryComp(uid, out MetaDataComponent? meta) || meta.Deleted)
            {
                remQueue.Add(uid);
                continue;
            }

            if (meta.EntityPaused)
                continue;

            remQueue.Add(uid);

            SpreadFluid(uid);
        }

        foreach (var removeUid in remQueue)
        {
            _fluidSpread.Remove(removeUid);
        }
    }

    private void SpreadFluid(EntityUid suid)
    {
        EntityUid GetOrCreate(EntityUid uid, string prototype, IMapGrid grid, Vector2i pos)
        {
            return uid == EntityUid.Invalid
                ? EntityManager.SpawnEntity(prototype, grid.GridTileToWorld(pos))
                : uid;
        }

        PuddleComponent? puddleComponent = null;
        MetaDataComponent? metadataOriginal = null;
        TransformComponent? transformOrig = null;
        FluidSpreaderComponent? spreader = null;

        if (!Resolve(suid, ref puddleComponent, ref metadataOriginal, ref transformOrig, ref spreader, false))
            return;

        var prototypeName = metadataOriginal.EntityPrototype!.ID;
        var visitedTiles = new HashSet<Vector2i>();

        if (!_mapManager.TryGetGrid(transformOrig.GridID, out var mapGrid))
            return;

        // skip origin puddle
        var nextToExpand = new List<PuddlePlacer>(9);
        ExpandPuddle(suid, visitedTiles, mapGrid, nextToExpand);

        while (nextToExpand.Count > 0
               && spreader.OverflownSolution.CurrentVolume > FixedPoint2.Zero)
        {
            // we need to clamp to prevent spreading 0u fluids, while never going over spill limit
            var divided = FixedPoint2.Clamp(spreader.OverflownSolution.CurrentVolume / nextToExpand.Count,
                FixedPoint2.Epsilon, puddleComponent.OverflowVolume);

            foreach (var posAndUid in nextToExpand)
            {
                var puddleUid = GetOrCreate(posAndUid.Uid, prototypeName, mapGrid, posAndUid.Pos);

                if (!TryComp(puddleUid, out PuddleComponent? puddle))
                    continue;

                posAndUid.Uid = puddleUid;

                if (puddle.CurrentVolume >= puddle.OverflowVolume) continue;

                // -puddle.OverflowLeft is guaranteed to be >= 0
                // iff puddle.CurrentVolume >= puddle.OverflowVolume
                var split = FixedPoint2.Min(divided, -puddle.OverflowLeft);
                _puddleSystem.TryAddSolution(
                    puddle.Owner,
                    spreader.OverflownSolution.SplitSolution(split),
                    false, false, puddle);

                // if solution is spent do not explore
                if (spreader.OverflownSolution.CurrentVolume <= FixedPoint2.Zero)
                    return;
            }

            // find edges
            nextToExpand = ExpandPuddles(nextToExpand, visitedTiles, mapGrid);
        }
    }

    private List<PuddlePlacer> ExpandPuddles(List<PuddlePlacer> toExpand,
        HashSet<Vector2i> visitedTiles,
        IMapGrid mapGrid)
    {
        var nextToExpand = new List<PuddlePlacer>(9);
        foreach (var puddlePlacer in toExpand)
        {
            ExpandPuddle(puddlePlacer.Uid, visitedTiles, mapGrid, nextToExpand, puddlePlacer.Pos);
        }

        return nextToExpand;
    }

    private void ExpandPuddle(EntityUid puddle,
        HashSet<Vector2i> visitedTiles,
        IMapGrid mapGrid,
        List<PuddlePlacer> nextToExpand,
        Vector2i? pos = null)
    {
        TransformComponent? transform = null;

        if (pos == null && !Resolve(puddle, ref transform, false))
        {
            return;
        }

        var puddlePos = pos ?? transform!.Coordinates.ToVector2i(EntityManager, _mapManager);

        // prepare next set of puddles to be expanded
        foreach (var direction in SharedDirectionExtensions.RandomDirections().ToArray())
        {
            var newPos = puddlePos.Offset(direction);
            if (visitedTiles.Contains(newPos))
                continue;

            visitedTiles.Add(newPos);

            if (CanExpand(newPos, mapGrid, out var uid))
                nextToExpand.Add(new PuddlePlacer(newPos, (EntityUid) uid));
        }
    }

    private bool CanExpand(Vector2i newPos, IMapGrid mapGrid,
        [NotNullWhen(true)] out EntityUid? uid)
    {
        if (!mapGrid.TryGetTileRef(newPos, out var tileRef)
            || tileRef.Tile.IsEmpty)
        {
            uid = null;
            return false;
        }

        foreach (var entity in mapGrid.GetAnchoredEntities(newPos))
        {
            IPhysBody? physics = null;
            PuddleComponent? existingPuddle = null;

            // This is an invalid location
            if (Resolve(entity, ref physics, false)
                && (physics.CollisionLayer & (int) CollisionGroup.Impassable) != 0)
            {
                uid = null;
                return false;
            }

            if (!Resolve(entity, ref existingPuddle, false))
                continue;

            uid = entity;
            return true;
        }

        uid = EntityUid.Invalid;
        return true;
    }
}

// Helper to allow mutable pair of (Pos, Uid)
internal sealed class PuddlePlacer
{
    internal Vector2i Pos;
    internal EntityUid Uid;

    public PuddlePlacer(Vector2i pos, EntityUid uid)
    {
        Pos = pos;
        Uid = uid;
    }
}
