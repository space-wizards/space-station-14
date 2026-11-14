using System.Numerics;
using Content.Server.Radiation.Components;
using Content.Server.Radiation.Events;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Radiation.Systems;

// main algorithm that fire radiation rays to target
public partial class RadiationSystem
{
    private void UpdateGridcast()
    {
        // should we save debug information into rays?
        // if there is no debug sessions connected - just ignore it
        var debug = _debugSessions.Count > 0;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        BuildSnapshots();

        var sourcesCount = _sourceSnapshots.Length;
        var receiversCount = _receiverSnapshots.Length;
        if (sourcesCount == 0 || receiversCount == 0)
        {
            UpdateGridcastDebugOverlay(
                stopwatch.Elapsed.TotalMilliseconds,
                sourcesCount,
                receiversCount,
                debug ? new List<DebugRadiationRay>() : null);

            RaiseLocalEvent(new RadiationSystemUpdatedEvent());
            return;
        }

        var results = new float[receiversCount];

        if (debug)
        {
            var debugRays = new List<DebugRadiationRay>();
            var nearbySources = new int[sourcesCount];

            for (var i = 0; i < receiversCount; i++)
                results[i] = CalculateRadiation(_receiverSnapshots[i], nearbySources, true, debugRays);

            UpdateGridcastDebugOverlay(stopwatch.Elapsed.TotalMilliseconds, sourcesCount, receiversCount, debugRays);
            ApplyRadiation(results);
        }
        else
        {
            var job = new RadiationJob(this, _receiverSnapshots, results);
            _parallel.ProcessNow(job, receiversCount);

            UpdateGridcastDebugOverlay(stopwatch.Elapsed.TotalMilliseconds, sourcesCount, receiversCount, null);
            ApplyRadiation(results);
        }

        RaiseLocalEvent(new RadiationSystemUpdatedEvent());
    }

    private void ApplyRadiation(float[] results)
    {
        for (var i = 0; i < _receiverSnapshots.Length; i++)
        {
            var receiver = _receiverSnapshots[i];
            var rads = MathF.Max(0f, results[i] - receiver.Resistance);

            if (!TryComp<RadiationReceiverComponent>(receiver.Uid, out var receiverComp))
                continue;

            receiverComp.CurrentRadiation = rads;

            if (rads > 0)
                IrradiateEntity(receiver.Uid, rads, GridcastUpdateRate);
        }
    }

    private float CalculateRadiation(
        in ReceiverSnapshot receiver,
        int[] nearbySources,
        bool saveVisitedTiles,
        List<DebugRadiationRay>? debugRays)
    {
        var rads = 0f;
        var nearbyCount = GetNearbySources(receiver.WorldPosition, nearbySources);

        for (var i = 0; i < nearbyCount; i++)
        {
            var source = _sourceSnapshots[nearbySources[i]];
            if (source.MapId != receiver.MapId)
                continue;

            var delta = source.WorldPosition - receiver.WorldPosition;
            if (delta.LengthSquared() > source.MaxRange * source.MaxRange)
                continue;

            var dist = delta.Length();
            var radsAfterDist = source.Intensity - source.Slope * dist;
            if (radsAfterDist < MinIntensity)
                continue;

            if (Irradiate(source, receiver, radsAfterDist, saveVisitedTiles) is not { } ray)
                continue;

            if (ray.ReachedDestination)
                rads += ray.Rads;

            if (saveVisitedTiles && debugRays is not null)
            {
                debugRays.Add(new DebugRadiationRay(
                    ray.MapId,
                    source.NetUid,
                    ray.Source,
                    receiver.NetUid,
                    ray.Destination,
                    ray.Rads,
                    ray.Blockers ?? new()));
            }
        }

        return rads;
    }

    private int GetNearbySources(Vector2 position, int[] nearbySources)
    {
        var count = _sourceSnapshots.Length;
        if (count <= SourceTreeThreshold)
        {
            for (var i = 0; i < count; i++)
                nearbySources[i] = i;

            return count;
        }

        var state = (nearbySources, 0, _sourceTree);

        _sourceTree.Query(ref state,
            static (ref (int[] Sources, int Count, B2DynamicTree<int> Tree) state, DynamicTree.Proxy proxy) =>
            {
                state.Sources[state.Count++] = state.Tree.GetUserData(proxy);
                return true;
            },
            position);

        return state.Item2;
    }

    private RadiationRay? Irradiate(
        in SourceSnapshot source,
        in ReceiverSnapshot receiver,
        float rads,
        bool saveVisitedTiles)
    {
        var ray = new RadiationRay(
            receiver.MapId,
            source.Uid,
            source.WorldPosition,
            receiver.Uid,
            receiver.WorldPosition,
            rads);

        if (GridcastSimplifiedSameGrid && receiver.GridUid is { } gridUid && source.GridUid == gridUid)
        {
            if (_gridSnapshotIndex.TryGetValue(gridUid, out var index))
                return Gridcast(_gridSnapshots[index], ref ray, saveVisitedTiles, source, receiver);

            return ray;
        }

        var box = Box2.FromTwoPoints(source.WorldPosition, receiver.WorldPosition);

        foreach (var grid in _gridSnapshots)
        {
            if (grid.MapId != receiver.MapId || !grid.WorldAABB.Intersects(box))
                continue;

            ray = Gridcast(grid, ref ray, saveVisitedTiles, source, receiver);
            if (ray.Rads <= 0)
                return ray;
        }

        return ray;
    }

    private RadiationRay Gridcast(
        in GridSnapshot grid,
        ref RadiationRay ray,
        bool saveVisitedTiles,
        in SourceSnapshot source,
        in ReceiverSnapshot receiver)
    {
        var resistanceMap = grid.Resistance;
        if (resistanceMap.Count == 0)
            return ray;

        var blockers = saveVisitedTiles ? new List<(Vector2i, float)>() : null;

        var srcLocal = source.ParentUid == grid.Uid
            ? source.LocalPosition
            : Vector2.Transform(ray.Source, grid.InvWorldMatrix);

        var dstLocal = receiver.ParentUid == grid.Uid
            ? receiver.LocalPosition
            : Vector2.Transform(ray.Destination, grid.InvWorldMatrix);

        Vector2i sourceGrid = new(
            (int)Math.Floor(srcLocal.X / grid.TileSize),
            (int)Math.Floor(srcLocal.Y / grid.TileSize));

        Vector2i destGrid = new(
            (int)Math.Floor(dstLocal.X / grid.TileSize),
            (int)Math.Floor(dstLocal.Y / grid.TileSize));

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
        ray.Blockers.Add(grid.NetUid, blockers);
        return ray;
    }

    private float GetRadiationResistance(EntityUid uid)
    {
        var child = uid;
        var xform = Transform(uid);
        var parent = xform.ParentUid;
        var resistance = 0f;

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

            if (_blockerQuery.TryComp(parent, out var blocker))
                resistance += blocker.RadResistance;

            child = parent;
            parent = parentXform.ParentUid;
        }

        return resistance;
    }

    private float GetAdjustedRadiationIntensity(EntityUid uid, float rads)
    {
        return MathF.Max(0f, rads - GetRadiationResistance(uid));
    }
}
