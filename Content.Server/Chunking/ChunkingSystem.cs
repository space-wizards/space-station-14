using System.Linq;
using Content.Shared.Decals;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Chunking;

/// <summary>
///     This system just exists to provide some utility functions for other systems that chunk data that needs to be
///     sent to players. In particular, see <see cref="GetChunksForSession"/>.
/// </summary>
public sealed class ChunkingSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    private Box2 _baseViewBounds;

    public override void Initialize()
    {
        base.Initialize();
        _xformQuery = GetEntityQuery<TransformComponent>();
        _configurationManager.OnValueChanged(CVars.NetMaxUpdateRange, OnPvsRangeChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _configurationManager.UnsubValueChanged(CVars.NetMaxUpdateRange, OnPvsRangeChanged);
    }

    private void OnPvsRangeChanged(float value)
    {
        _baseViewBounds = Box2.UnitCentered.Scale(value);
    }

    public Dictionary<NetEntity, HashSet<Vector2i>> GetChunksForSession(
        ICommonSession session,
        int chunkSize,
        ObjectPool<HashSet<Vector2i>> indexPool,
        ObjectPool<Dictionary<NetEntity, HashSet<Vector2i>>> viewerPool,
        float? viewEnlargement = null)
    {
        var chunks = viewerPool.Get();
        DebugTools.Assert(chunks.Count == 0);

        if (session.Status != SessionStatus.InGame || session.AttachedEntity is not {} player)
            return chunks;

        var enlargement = viewEnlargement ?? chunkSize;
        AddViewerChunks(player, chunks, indexPool, chunkSize, enlargement);
        foreach (var uid in session.ViewSubscriptions)
        {
            AddViewerChunks(uid, chunks, indexPool, chunkSize, enlargement);
        }

        return chunks;
    }

    private void AddViewerChunks(EntityUid viewer,
        Dictionary<NetEntity, HashSet<Vector2i>> chunks,
        ObjectPool<HashSet<Vector2i>> indexPool,
        int chunkSize,
        float viewEnlargement)
    {
        if (!_xformQuery.TryGetComponent(viewer, out var xform))
            return;

        var pos = _transform.GetWorldPosition(xform);
        var bounds = _baseViewBounds.Translated(pos).Enlarged(viewEnlargement);

        var state = (chunks, indexPool, chunkSize, bounds);
        _mapManager.FindGridsIntersecting(xform.MapID, bounds, ref state, AddGridChunks, true);
    }

    private bool AddGridChunks(
        EntityUid uid,
        MapGridComponent grid,
        ref (Dictionary<NetEntity, HashSet<Vector2i>>, ObjectPool<HashSet<Vector2i>>, int, Box2) state)
    {
        var netGrid = GetNetEntity(uid);

        var (chunks, pool, size, bounds) = state;
        if (!chunks.TryGetValue(netGrid, out var set))
        {
            chunks[netGrid] = set = pool.Get();
            DebugTools.Assert(set.Count == 0);
        }

        var enumerator = new ChunkIndicesEnumerator(_transform.GetInvWorldMatrix(uid).TransformBox(bounds), size);
        while (enumerator.MoveNext(out var indices))
        {
            set.Add(indices.Value);
        }

        return true;
    }
}

