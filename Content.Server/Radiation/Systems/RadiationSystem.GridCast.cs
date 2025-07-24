using System.Collections.Concurrent;
using System.Linq;
using System.Numerics;
using Content.Server.Radiation.Components;
using Content.Server.Radiation.Events;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Map.Components;
using Robust.Shared.Threading;
using Robust.Shared.Utility;

namespace Content.Server.Radiation.Systems
{
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
            // should we save debug information into rays?
            // if there is no debug sessions connected - just ignore it
            var debug = _debugSessions.Count > 0;
            var stopwatch = new Robust.Shared.Timing.Stopwatch();
            stopwatch.Start();

            _sources.Clear();
            _sources.EnsureCapacity(Count<RadiationSourceComponent>());
            var sourcesQuery = EntityQueryEnumerator<RadiationSourceComponent, TransformComponent>();
            while (sourcesQuery.MoveNext(out var uid, out var source, out var xform))
            {
                if (!source.Enabled)
                    continue;

                var worldPos = _transform.GetWorldPosition(xform);
                // Intensity is scaled by stack size.
                var intensity = source.Intensity * _stack.GetCount(uid);
                intensity = GetAdjustedRadiationIntensity(uid, intensity);

                var maxRange = source.Slope > 1e-6f ? intensity / source.Slope : float.MaxValue;
                _sources.Add(new SourceData(intensity, source.Slope, maxRange, (uid, source, xform), worldPos));
            }

            var destinationsQuery = EntityQueryEnumerator<RadiationReceiverComponent, TransformComponent>();
            var destinations = new ValueList<(EntityUid Uid, TransformComponent Xform)>();
            while (destinationsQuery.MoveNext(out var uid, out _, out var xform))
            {
                destinations.Add((uid, xform));
            }

            if (destinations.Count == 0 || _sources.Count == 0)
            {
                UpdateGridcastDebugOverlay(stopwatch.Elapsed.TotalMilliseconds, _sources.Count, destinations.Count, null);
                RaiseLocalEvent(new RadiationSystemUpdatedEvent());
                return;
            }

            var results = new float[destinations.Count];
            var debugRays = debug ? new ConcurrentBag<DebugRadiationRay>() : null;

            var job = new RadiationJob
            {
                System = this,
                Sources = _sources,
                Destinations = destinations,
                Results = results,
                DebugRays = debugRays,
                Debug = debug
            };

            _parallel.ProcessNow(job, destinations.Count);

            for (var i = 0; i < destinations.Count; i++)
            {
                var (uid, _) = destinations[i];
                var rads = results[i];

                if (Deleted(uid) || !TryComp<RadiationReceiverComponent>(uid, out var receiver))
                    continue;

                receiver.CurrentRadiation = rads;
                if (rads > 0)
                    IrradiateEntity(uid, rads, GridcastUpdateRate);
            }

            UpdateGridcastDebugOverlay(stopwatch.Elapsed.TotalMilliseconds, _sources.Count, destinations.Count, debugRays?.ToList());

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

            if (dist > source.MaxRange)
                return null;

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

        Vector2i sourceGrid = new(
            (int)Math.Floor(srcLocal.X / grid.Comp1.TileSize),
            (int)Math.Floor(srcLocal.Y / grid.Comp1.TileSize));

        Vector2i destGrid = new(
            (int)Math.Floor(dstLocal.X / grid.Comp1.TileSize),
            (int)Math.Floor(dstLocal.Y / grid.Comp1.TileSize));

            var line = new GridLineEnumerator(sourceGrid, destGrid);
            while (line.MoveNext())
            {
                var point = line.Current;
                if (!resistanceMap.TryGetValue(point, out var resData))
                    continue;

                ray.Rads -= resData;
                if (saveVisitedTiles)
                    blockers!.Add((point, ray.Rads));

                if (ray.Rads <= MinIntensity)
                {
                    ray.Rads = 0;
                    break;
                }
            }

            if (!saveVisitedTiles || blockers!.Count <= 0)
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
            public required List<SourceData> Sources { get; init; }
            public required ValueList<(EntityUid Uid, TransformComponent Xform)> Destinations { get; init; }
            public required float[] Results { get; init; }
            public required ConcurrentBag<DebugRadiationRay>? DebugRays { get; init; }
            public required bool Debug { get; init; }

            public void Execute(int index)
            {
                var (destUid, destTrs) = Destinations[index];
                var gridList = new List<Entity<MapGridComponent>>();
                var destWorld = System._transform.GetWorldPosition(destTrs);
                var rads = 0f;
                var destMapId = destTrs.MapID;

                foreach (var source in Sources)
                {
                    if (source.Transform.MapID != destMapId)
                        continue;

                    var delta = source.WorldPosition - destWorld;
                    if (delta.LengthSquared() > source.MaxRange * source.MaxRange)
                        continue;

                    if (System.Irradiate(source, destUid, destTrs, destWorld, Debug, gridList) is not { } ray)
                        continue;

                    if (ray.ReachedDestination)
                        rads += ray.Rads;

                    if (Debug)
                    {
                        DebugRays!.Add(new DebugRadiationRay(
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
}
