using System.Numerics;
using Content.Server.Radiation.Components;
using Content.Server.Radiation.Events;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Radiation.Systems;

// Main algorithm that fires radiation rays at targets.
public partial class RadiationSystem
{
    private float[] _results = Array.Empty<float>();

    private void UpdateGridcast()
    {
        // Should we save debug information into rays?
        // If no debug sessions are connected, ignore it.
        var saveDebugInfo = _debugSessions.Count > 0;

        var stopwatch = RStopwatch.StartNew();

        BuildSourceSnapshots(saveDebugInfo);

        var sourcesCount = _sourceCount;
        if (sourcesCount == 0)
        {
            var clearedReceivers = ClearRadiation();

            UpdateGridcastDebugOverlay(
                stopwatch.Elapsed.TotalMilliseconds,
                sourcesCount,
                clearedReceivers,
                null);

            RaiseLocalEvent(new RadiationSystemUpdatedEvent());
            return;
        }

        BuildReceiverSnapshots(saveDebugInfo);

        var receiversCount = _receiverCount;
        if (receiversCount == 0)
        {
            UpdateGridcastDebugOverlay(
                stopwatch.Elapsed.TotalMilliseconds,
                sourcesCount,
                receiversCount,
                null);

            RaiseLocalEvent(new RadiationSystemUpdatedEvent());
            return;
        }

        BuildGridSnapshots(saveDebugInfo);
        BuildSourceTree();

        if (_results.Length < receiversCount)
            _results = new float[receiversCount];

        Array.Clear(_results, 0, receiversCount);

        List<DebugRadiationRay>[]? debugRays = null;
        if (saveDebugInfo)
        {
            debugRays = new List<DebugRadiationRay>[receiversCount];

            for (var i = 0; i < receiversCount; i++)
                debugRays[i] = new List<DebugRadiationRay>();
        }

        var job = new RadiationJob(this, _receiverSnapshots, _results, debugRays);
        _parallel.ProcessNow(job, receiversCount);

        UpdateGridcastDebugOverlay(
            stopwatch.Elapsed.TotalMilliseconds,
            sourcesCount,
            receiversCount,
            debugRays is null ? null : CollectDebugRays(debugRays));

        ApplyRadiation(_results);

        RaiseLocalEvent(new RadiationSystemUpdatedEvent());
    }

    private static List<DebugRadiationRay> CollectDebugRays(List<DebugRadiationRay>[] debugRays)
    {
        var rays = new List<DebugRadiationRay>();

        foreach (var receiverRays in debugRays)
            rays.AddRange(receiverRays);

        return rays;
    }

    private int ClearRadiation()
    {
        var count = 0;
        var query = EntityQueryEnumerator<RadiationReceiverComponent, TransformComponent>();

        while (query.MoveNext(out _, out var receiver, out _))
        {
            receiver.CurrentRadiation = 0;
            count++;
        }

        return count;
    }

    private void ApplyRadiation(float[] results)
    {
        for (var i = 0; i < _receiverCount; i++)
        {
            ref readonly var receiver = ref _receiverSnapshots[i];
            results[i] = GetAdjustedRadiationIntensity(receiver.Uid, results[i]);
        }

        for (var i = 0; i < _receiverCount; i++)
        {
            ref readonly var receiver = ref _receiverSnapshots[i];
            var rads = results[i];

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
        List<DebugRadiationRay>? debugRays)
    {
        var saveVisitedTiles = debugRays is not null;
        var rads = 0f;
        var nearbyCount = GetNearbySources(receiver.WorldPosition, nearbySources);

        for (var i = 0; i < nearbyCount; i++)
        {
            ref readonly var source = ref _sourceSnapshots[nearbySources[i]];
            if (source.MapId != receiver.MapId)
                continue;

            var delta = source.WorldPosition - receiver.WorldPosition;
            if (delta.LengthSquared() > source.MaxRange * source.MaxRange)
                continue;

            var dist = delta.Length();
            var radsAfterDist = source.Intensity - source.Slope * dist;
            if (!float.IsFinite(radsAfterDist) || radsAfterDist < MinIntensity)
                continue;

            var ray = Irradiate(source, receiver, radsAfterDist, saveVisitedTiles);

            if (ray.ReachedDestination)
                rads += ray.Rads;

            if (debugRays is not null)
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
        var state = (Sources: nearbySources, Count: 0, Tree: _sourceTree);

        _sourceTree.Query(ref state,
            static (ref (int[] Sources, int Count, B2DynamicTree<int> Tree) state, DynamicTree.Proxy proxy) =>
            {
                if (state.Count >= state.Sources.Length)
                {
                    DebugTools.Assert(false, "Radiation source tree returned more proxies than snapshots.");
                    return false;
                }

                state.Sources[state.Count++] = state.Tree.GetUserData(proxy);
                return true;
            },
            position);

        return state.Count;
    }

    /// <summary>
    /// Casts a radiation ray. The caller must have already validated the map, distance and minimum intensity.
    /// </summary>
    private RadiationRay Irradiate(
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

        for (var i = 0; i < _gridCount; i++)
        {
            ref readonly var grid = ref _gridSnapshots[i];
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

        // TODO: Assumes grids are parented directly to the map (local matrix == world matrix).
        var srcLocal = source.ParentUid == grid.Uid
            ? source.LocalPosition
            : Vector2.Transform(ray.Source, grid.InvLocalMatrix);

        var dstLocal = receiver.ParentUid == grid.Uid
            ? receiver.LocalPosition
            : Vector2.Transform(ray.Destination, grid.InvLocalMatrix);

        if (!TryGetTileIndices(srcLocal, grid.TileSize, out var sourceGrid) ||
            !TryGetTileIndices(dstLocal, grid.TileSize, out var destGrid))
        {
            return ray;
        }

        var line = new GridLineEnumerator(sourceGrid, destGrid);
        while (line.MoveNext())
        {
            var point = line.Current;
            if (!resistanceMap.TryGetValue(point, out var resData))
                continue;

            ray.Rads -= resData;

            if (blockers is not null)
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

    private static bool TryGetTileIndices(Vector2 local, ushort tileSize, out Vector2i indices)
    {
        indices = default;

        if (tileSize == 0)
            return false;

        var x = local.X / tileSize;
        var y = local.Y / tileSize;

        if (!float.IsFinite(x) ||
            !float.IsFinite(y) ||
            MathF.Abs(x) > PhysicsConstants.Huge ||
            MathF.Abs(y) > PhysicsConstants.Huge)
        {
            return false;
        }

        indices = new Vector2i((int)MathF.Floor(x), (int)MathF.Floor(y));
        return true;
    }

    // TODO: Nested containers are handled incorrectly: the nearest container is counted repeatedly
    // and outer containers are ignored. Fixing this changes gameplay and is tracked separately.
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

            if (_blockerQuery.TryComp(xform.ParentUid, out var blocker) &&
                float.IsFinite(blocker.RadResistance) &&
                blocker.RadResistance > 0f)
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
}
