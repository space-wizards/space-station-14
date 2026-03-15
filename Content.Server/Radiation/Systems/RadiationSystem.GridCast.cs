using System.Collections.Concurrent;
using System.Linq;
using System.Numerics;
using Content.Server.Radiation.Components;
using Content.Server.Radiation.Events;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Threading;
using Robust.Shared.Utility;

namespace Content.Server.Radiation.Systems;

// main algorithm that fire radiation rays to target
public partial class RadiationSystem
{
    private readonly record struct SourceData(
        float Intensity,
        float Slope,
        float MaxRange,
        Entity<RadiationSourceComponent, TransformComponent> Entity,
        Vector2 WorldPosition)
    {
        public EntityUid Uid => Entity.Owner;
        public TransformComponent Transform => Entity.Comp2;
    }

    private void UpdateGridcast()
    {
        var debug = _debugSessions.Count > 0;
        var stopwatch = new Robust.Shared.Timing.Stopwatch();
        stopwatch.Start();

        var sourcesCount = _sourceDataMap.Count;
        if (_activeReceivers.Count == 0 || sourcesCount == 0)
        {
            RaiseLocalEvent(new RadiationSystemUpdatedEvent());
            return;
        }

        var results = new float[_activeReceivers.Count];
        var debugRays = debug ? new ConcurrentBag<DebugRadiationRay>() : null;

        var job = new RadiationJob
        {
            System = this,
            SourceTree = _sourceTree,
            SourceDataMap = _sourceDataMap,
            Destinations = _activeReceivers,
            Results = results,
            DebugRays = debugRays,
            Debug = debug
        };

        _parallel.ProcessNow(job, _activeReceivers.Count);

        for (var i = 0; i < _activeReceivers.Count; i++)
        {
            var uid = _activeReceivers[i];
            var rads = results[i];

            if (TryComp<RadiationReceiverComponent>(uid, out var receiver))
            {
                receiver.CurrentRadiation = rads;
                if (rads > 0)
                    IrradiateEntity(uid, rads, GridcastUpdateRate);
            }
        }

        if (debugRays is not null)
            UpdateGridcastDebugOverlay(stopwatch.Elapsed.TotalMilliseconds, sourcesCount, _activeReceivers.Count, debugRays.ToList());

        RaiseLocalEvent(new RadiationSystemUpdatedEvent());
    }

    private RadiationRay? Irradiate(SourceData source,
        EntityUid destUid,
        TransformComponent destTrs,
        Vector2 destWorld,
        bool saveVisitedTiles,
        List<Entity<MapGridComponent>> gridList)
    {
        var mapId = destTrs.MapID;
        var dist = (destWorld - source.WorldPosition).Length();
        var rads = source.Intensity - source.Slope * dist;
        if (rads < MinIntensity)
            return null;

        var ray = new RadiationRay(mapId, source.Entity, source.WorldPosition, destUid, destWorld, rads);

        var box = Box2.FromTwoPoints(source.WorldPosition, destWorld);
        gridList.Clear();
        _mapManager.FindGridsIntersecting(mapId, box, ref gridList, true);

        foreach (var grid in gridList)
        {
            ray = Gridcast((grid.Owner, grid.Comp, Transform(grid)), ref ray, saveVisitedTiles, source.Transform, destTrs);
            if (ray.Rads <= 0)
                return ray;
        }

        return ray;
    }

    private RadiationRay Gridcast(
        Entity<MapGridComponent, TransformComponent> grid,
        ref RadiationRay ray,
        bool saveVisitedTiles,
        TransformComponent sourceTrs,
        TransformComponent destTrs)
    {
        var blockers = saveVisitedTiles ? new List<(Vector2i, float)>() : null;
        var gridUid = grid.Owner;
        if (!_resistanceQuery.TryGetComponent(gridUid, out var resistance))
            return ray;

        var resistanceMap = resistance.ResistancePerTile;

        Vector2 srcLocal = sourceTrs.ParentUid == grid.Owner
            ? sourceTrs.LocalPosition
            : Vector2.Transform(ray.Source, grid.Comp2.InvLocalMatrix);

        Vector2 dstLocal = destTrs.ParentUid == grid.Owner
            ? destTrs.LocalPosition
            : Vector2.Transform(ray.Destination, grid.Comp2.InvLocalMatrix);

        Vector2i sourceGrid = new((int)Math.Floor(srcLocal.X / grid.Comp1.TileSize), (int)Math.Floor(srcLocal.Y / grid.Comp1.TileSize));
        Vector2i destGrid = new((int)Math.Floor(dstLocal.X / grid.Comp1.TileSize), (int)Math.Floor(dstLocal.Y / grid.Comp1.TileSize));

        var line = new GridLineEnumerator(sourceGrid, destGrid);
        while (line.MoveNext())
        {
            var point = line.Current;
            if (!resistanceMap.TryGetValue(point, out var resData))
                continue;

            ray.Rads -= resData;
            if (saveVisitedTiles && blockers is not null)
                blockers.Add((point, ray.Rads));

            if (ray.Rads <= MinIntensity)
            {
                ray.Rads = 0;
                break;
            }
        }

        if (blockers is null || blockers.Count == 0)
            return ray;

        ray.Blockers ??= new();
        ray.Blockers.Add(GetNetEntity(gridUid), blockers);
        return ray;
    }

    private float GetAdjustedRadiationIntensity(EntityUid uid, float rads)
    {
        var child = uid;
        var xform = Transform(uid);
        var parent = xform.ParentUid;

        while (parent.IsValid())
        {
            var parentXform = Transform(parent);
            var childMeta = MetaData(child);

            if ((childMeta.Flags & MetaDataFlags.InContainer) != MetaDataFlags.InContainer)
            {
                child = parent;
                parent = parentXform.ParentUid;
                continue;
            }

            if (_blockerQuery.TryComp(xform.ParentUid, out var blocker))
            {
                rads -= blocker.RadResistance;
                if (rads < 0)
                    return 0;
            }

            child = parent;
            parent = parentXform.ParentUid;
        }

        return rads;
    }

    [UsedImplicitly]
    private readonly record struct RadiationJob : IParallelRobustJob
    {
        public int BatchSize => 5;
        public required RadiationSystem System { get; init; }
        public required B2DynamicTree<EntityUid> SourceTree { get; init; }
        public required Dictionary<EntityUid, SourceData> SourceDataMap { get; init; }
        public required List<EntityUid> Destinations { get; init; }
        public required float[] Results { get; init; }
        public required ConcurrentBag<DebugRadiationRay>? DebugRays { get; init; }
        public required bool Debug { get; init; }

        public void Execute(int index)
        {
            var destUid = Destinations[index];
            if (System.Deleted(destUid) || !System.TryComp(destUid, out TransformComponent? destTrs))
            {
                Results[index] = 0;
                return;
            }

            var gridList = RadiationSystem._gridListCache.Value!;
            var nearbySources = RadiationSystem._nearbySourcesCache.Value!;
            nearbySources.Clear();
            var destWorld = System._transform.GetWorldPosition(destTrs);
            var rads = 0f;
            var destMapId = destTrs.MapID;
            var queryAabb = new Box2(destWorld, destWorld);

            var state = (nearbySources, SourceTree);
            SourceTree.Query(ref state, static (ref (List<EntityUid> nearby, B2DynamicTree<EntityUid> tree) tuple, DynamicTree.Proxy proxy) =>
            {
                var uid = tuple.tree.GetUserData(proxy);
                tuple.nearby.Add(uid);
                return true;
            }, in queryAabb);

            foreach (var sourceUid in nearbySources)
            {
                if (!SourceDataMap.TryGetValue(sourceUid, out var source)
                    || source.Transform.MapID != destMapId) continue;
                var delta = source.WorldPosition - destWorld;
                if (delta.LengthSquared() > source.MaxRange * source.MaxRange) continue;
                var dist = delta.Length();
                var radsAfterDist = source.Intensity - source.Slope * dist;
                if (radsAfterDist < System.MinIntensity) continue;
                if (System.Irradiate(source, destUid, destTrs, destWorld, Debug, gridList) is not { } ray) continue;

                if (ray.ReachedDestination)
                    rads += ray.Rads;

                if (DebugRays is not null)
                {
                    DebugRays.Add(new DebugRadiationRay(
                        ray.MapId,
                        System.GetNetEntity(ray.SourceUid),
                        ray.Source,
                        System.GetNetEntity(ray.DestinationUid),
                        ray.Destination,
                        ray.Rads,
                        ray.Blockers ?? new())
                    );
                }
            }

            rads = System.GetAdjustedRadiationIntensity(destUid, rads);
            Results[index] = rads;
        }
    }
}
