using Content.Shared.Decals;
using Microsoft.Extensions.ObjectPool;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
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

    // Pool if we ever parallelise.
    private HashSet<EntityUid> _viewers = new(64);

    public static Vector2i GetChunkIndices(Vector2 coordinates, float chunkSize)
        => new((int) Math.Floor(coordinates.X / chunkSize), (int) Math.Floor(coordinates.Y / chunkSize));

    private Box2 _baseViewBounds;

    public override void Initialize()
    {
        base.Initialize();
        _configurationManager.OnValueChanged(CVars.NetMaxUpdateRange, OnPvsRangeChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _configurationManager.UnsubValueChanged(CVars.NetMaxUpdateRange, OnPvsRangeChanged);
    }

    private void OnPvsRangeChanged(float value) => _baseViewBounds = Box2.UnitCentered.Scale(value);

    public Dictionary<EntityUid, HashSet<Vector2i>> GetChunksForSession(
        IPlayerSession session,
        int chunkSize,
        EntityQuery<TransformComponent> xformQuery,
        ObjectPool<HashSet<Vector2i>>? indexPool = null,
        ObjectPool<Dictionary<EntityUid, HashSet<Vector2i>>>? viewerPool = null,
        float? viewEnlargement = null)
    {
        var viewers = GetSessionViewers(session);
        var chunks = GetChunksForViewers(viewers, chunkSize, indexPool, viewerPool, viewEnlargement ?? chunkSize / 2, xformQuery);
        viewers.Clear();
        return chunks;
    }

    private HashSet<EntityUid> GetSessionViewers(IPlayerSession session)
    {
        var viewers = _viewers;
        if (session.Status != SessionStatus.InGame || session.AttachedEntity is null)
            return viewers;

        viewers.Add(session.AttachedEntity.Value);

        foreach (var uid in session.ViewSubscriptions)
        {
            viewers.Add(uid);
        }

        return viewers;
    }

    private Dictionary<EntityUid, HashSet<Vector2i>> GetChunksForViewers(
        HashSet<EntityUid> viewers,
        int chunkSize,
        ObjectPool<HashSet<Vector2i>>? indexPool,
        ObjectPool<Dictionary<EntityUid, HashSet<Vector2i>>>? viewerPool,
        float viewEnlargement,
        EntityQuery<TransformComponent> xformQuery)
    {
        Dictionary<EntityUid, HashSet<Vector2i>> chunks = viewerPool?.Get() ?? new();
        DebugTools.Assert(chunks.Count == 0);

        foreach (var viewerUid in viewers)
        {
            var xform = xformQuery.GetComponent(viewerUid);
            var pos = _transform.GetWorldPosition(xform, xformQuery);
            var bounds = _baseViewBounds.Translated(pos).Enlarged(viewEnlargement);

            foreach (var grid in _mapManager.FindGridsIntersecting(xform.MapID, bounds))
            {
                if (!chunks.TryGetValue(grid.GridEntityId, out var set))
                {
                    chunks[grid.GridEntityId] = set = indexPool?.Get() ?? new();
                    DebugTools.Assert(set.Count == 0);
                }

                var enumerator = new ChunkIndicesEnumerator(_transform.GetInvWorldMatrix(grid.GridEntityId, xformQuery).TransformBox(bounds), chunkSize);

                while (enumerator.MoveNext(out var indices))
                {
                    set.Add(indices.Value);
                }
            }
        }

        return chunks;
    }
}

