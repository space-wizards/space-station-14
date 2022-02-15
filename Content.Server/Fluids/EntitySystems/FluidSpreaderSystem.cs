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

        if (puddleSolution.CurrentVolume <= puddleComponent.OverflowVolume)
            return;

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
            MetaDataComponent? meta = null;

            if (Paused(uid, meta))
                continue;

            // If not paused
            // it's either Deleted or will be via SpreadFluid
            remQueue.Add(uid);

            if (Deleted(uid, meta))
                continue;

            SpreadFluid(uid);
        }

        foreach (var removeUid in remQueue)
        {
            _fluidSpread.Remove(removeUid);
        }
    }

    private void SpreadFluid(EntityUid suid)
    {
        PuddleComponent? puddleComponent = null;
        MetaDataComponent? metadataOriginal = null;
        TransformComponent? transformOrig = null;
        FluidSpreaderComponent? spreader = null;

        if (!Resolve(suid, ref puddleComponent, ref metadataOriginal, ref transformOrig, ref spreader, false))
            return;

        var prototypeName = metadataOriginal.EntityPrototype!.ID;

        var puddles = new List<PuddleComponent> { puddleComponent };
        var visitedTiles = new HashSet<Vector2i>();

        if (!_mapManager.TryGetGrid(transformOrig.GridID, out var mapGrid))
            return;

        while (puddles.Count > 0
               && spreader.OverflownSolution.CurrentVolume > FixedPoint2.Zero)
        {
            var nextToExpand = new List<(Vector2i, EntityUid?)>();

            var divided = spreader.OverflownSolution.CurrentVolume / puddles.Count;

            foreach (var puddle in puddles)
            {
                if (puddle.CurrentVolume >= puddle.OverflowVolume) continue;

                // -puddle.OverflowLeft is guaranteed to be >= 0
                // iff puddle.CurrentVolume >= puddle.OverflowVolume
                var split = FixedPoint2.Min(divided, -puddle.OverflowLeft);
                _puddleSystem.TryAddSolution(
                    puddle.Owner,
                    spreader.OverflownSolution.SplitSolution(split),
                    false, false, puddle);
            }

            // if solution is spent do not explore
            if (spreader.OverflownSolution.CurrentVolume <= FixedPoint2.Zero)
                continue;

            // find edges
            foreach (var puddle in puddles)
            {
                TransformComponent? transform = null;

                if (!Resolve(puddle.Owner, ref transform, false))
                    continue;

                // prepare next set of puddles to be expanded
                var puddlePos = transform.Coordinates.ToVector2i(EntityManager, _mapManager);
                foreach (var direction in SharedDirectionExtensions.RandomDirections().ToArray())
                {
                    var newPos = puddlePos.Offset(direction);
                    if (visitedTiles.Contains(newPos))
                        continue;

                    visitedTiles.Add(newPos);

                    if (CanExpand(newPos, mapGrid, out var uid))
                        nextToExpand.Add((newPos, uid));
                }
            }

            puddles = new List<PuddleComponent>();

            // prepare edges for next iteration
            foreach (var (pos, uid) in nextToExpand)
            {
                if (spreader.OverflownSolution.CurrentVolume <= FixedPoint2.Zero)
                    continue;

                var puddleUid = uid!.Value;
                var coordinate = mapGrid.GridTileToWorld(pos);
                if (uid == EntityUid.Invalid)
                {
                    puddleUid = EntityManager.SpawnEntity(prototypeName, coordinate);
                }

                puddles.Add(EntityManager.GetComponent<PuddleComponent>(puddleUid));
            }
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
