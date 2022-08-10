using Content.Shared.Decals;
using Microsoft.Extensions.ObjectPool;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using System.Linq;

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
        ObjectPool<HashSet<Vector2i>> indexPool,
        ObjectPool<Dictionary<EntityUid, HashSet<Vector2i>>> viewerPool,
        float? viewEnlargement = null)
    {
        var viewers = GetSessionViewers(session);
        var chunks = GetChunksForViewers(viewers, chunkSize, indexPool, viewerPool, viewEnlargement ?? chunkSize, xformQuery);
        return chunks;
    }

    private HashSet<EntityUid> GetSessionViewers(IPlayerSession session)
    {
        var viewers = new HashSet<EntityUid>();
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
        ObjectPool<HashSet<Vector2i>> indexPool,
        ObjectPool<Dictionary<EntityUid, HashSet<Vector2i>>> viewerPool,
        float viewEnlargement,
        EntityQuery<TransformComponent> xformQuery)
    {
        Dictionary<EntityUid, HashSet<Vector2i>> chunks = viewerPool.Get();
        DebugTools.Assert(chunks.Count == 0);

        foreach (var viewerUid in viewers)
        {
            if (!xformQuery.TryGetComponent(viewerUid, out var xform))
            {
                Logger.Error($"Player has deleted viewer entities? Viewers: {string.Join(", ", viewers.Select(x => ToPrettyString(x)))}");
                continue;
            }

            var pos = _transform.GetWorldPosition(xform, xformQuery);
            var bounds = _baseViewBounds.Translated(pos).Enlarged(viewEnlargement);

            foreach (var grid in _mapManager.FindGridsIntersecting(xform.MapID, bounds, true))
            {
                if (!chunks.TryGetValue(grid.GridEntityId, out var set))
                {
                    chunks[grid.GridEntityId] = set = indexPool.Get();
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

