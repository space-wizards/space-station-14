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
using ChunkIndicesEnumerator = Robust.Shared.Map.Enumerators.ChunkIndicesEnumerator;

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
        Subs.CVar(_configurationManager, CVars.NetMaxUpdateRange, OnPvsRangeChanged, true);
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

        var state = new QueryState(chunks, indexPool, chunkSize, bounds, _transform, EntityManager);
        _mapManager.FindGridsIntersecting(xform.MapID, bounds, ref state, AddGridChunks, true);
    }

    private static bool AddGridChunks(
        EntityUid uid,
        MapGridComponent grid,
        ref QueryState state)
    {
        var netGrid = state.EntityManager.GetNetEntity(uid);
        if (!state.Chunks.TryGetValue(netGrid, out var set))
        {
            state.Chunks[netGrid] = set = state.Pool.Get();
            DebugTools.Assert(set.Count == 0);
        }

        var aabb = state.Transform.GetInvWorldMatrix(uid).TransformBox(state.Bounds);
        var enumerator = new ChunkIndicesEnumerator(aabb, state.ChunkSize);
        while (enumerator.MoveNext(out var indices))
        {
            set.Add(indices.Value);
        }

        return true;
    }

    private readonly struct QueryState
    {
        public readonly Dictionary<NetEntity, HashSet<Vector2i>> Chunks;
        public readonly ObjectPool<HashSet<Vector2i>> Pool;
        public readonly int ChunkSize;
        public readonly Box2 Bounds;
        public readonly SharedTransformSystem Transform;
        public readonly EntityManager EntityManager;

        public QueryState(
            Dictionary<NetEntity, HashSet<Vector2i>> chunks,
            ObjectPool<HashSet<Vector2i>> pool,
            int chunkSize,
            Box2 bounds,
            SharedTransformSystem transform,
            EntityManager entityManager)
        {
            Chunks = chunks;
            Pool = pool;
            ChunkSize = chunkSize;
            Bounds = bounds;
            Transform = transform;
            EntityManager = entityManager;
        }
    }
}

