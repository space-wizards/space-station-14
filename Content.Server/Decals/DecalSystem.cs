using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared.Chunking;
using Content.Shared.Decals;
using Microsoft.Extensions.ObjectPool;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Threading;
using Robust.Shared.Utility;
using System.Numerics;
using static Content.Shared.Decals.DecalGridComponent;

namespace Content.Server.Decals;

public sealed class DecalSystem : SharedDecalSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IParallelManager _parMan = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ChunkingSystem _chunking = default!;

    private UpdatePlayerJob _updateJob;

    private readonly Dictionary<NetEntity, HashSet<Vector2i>> _dirtyChunks = new();
    private static readonly Vector2 _boundsMinExpansion = new(0.01f, 0.01f);
    private static readonly Vector2 _boundsMaxExpansion = new(1.01f, 1.01f);
    private List<ICommonSession> Sessions = new();

    // If this ever gets parallelised then you'll want to increase the pooled count.
    private ObjectPool<HashSet<Vector2i>> _chunkIndexPool =
        new DefaultObjectPool<HashSet<Vector2i>>(
            new DefaultPooledObjectPolicy<HashSet<Vector2i>>(), 64);

    private ObjectPool<Dictionary<NetEntity, HashSet<Vector2i>>> _chunkViewerPool =
        new DefaultObjectPool<Dictionary<NetEntity, HashSet<Vector2i>>>(
            new DefaultPooledObjectPolicy<Dictionary<NetEntity, HashSet<Vector2i>>>(), 64);

    public override void Initialize()
    {
        base.Initialize();

        _updateJob = new UpdatePlayerJob()
        {
            System = this,
            Sessions = Sessions,
        };

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        SubscribeLocalEvent<PostGridSplitEvent>(OnGridSplit);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        switch (e.NewStatus)
        {
            case SessionStatus.InGame:
                Sessions.Add(e.Session);
                PreviousSentChunks[e.Session] = new();
                break;
            case SessionStatus.Disconnected:
                Sessions.Remove(e.Session);
                PreviousSentChunks.Remove(e.Session);
                break;
        }
    }

    private void OnGridSplit(ref PostGridSplitEvent ev)
    {
        if (!TryComp(ev.OldGrid, out DecalGridComponent? oldComp))
            return;

        if (!TryComp(ev.Grid, out DecalGridComponent? newComp))
            return;

        // Transfer decals over to the new grid.
        var enumerator = MapSystem.GetAllTilesEnumerator(ev.Grid, Comp<MapGridComponent>(ev.Grid));

        var oldChunkCollection = oldComp.ChunkCollection.ChunkCollection;
        var chunkCollection = newComp.ChunkCollection.ChunkCollection;

        while (enumerator.MoveNext(out var tile))
        {
            var tilePos = (Vector2) tile.Value.GridIndices;
            var chunkIndices = GetChunkIndices(tilePos);

            if (!oldChunkCollection.TryGetValue(chunkIndices, out var oldChunk))
                continue;

            var bounds = new Box2(tilePos - _boundsMinExpansion, tilePos + _boundsMaxExpansion);
            var toRemove = new RemQueue<uint>();

            foreach (var (oldDecalId, decal) in oldChunk.Decals)
            {
                if (!bounds.Contains(decal.Coordinates))
                    continue;

                var newDecalId = newComp.ChunkCollection.NextDecalId++;
                var newChunk = chunkCollection.GetOrNew(chunkIndices);
                newChunk.Decals[newDecalId] = decal;
                newComp.DecalIndex[newDecalId] = chunkIndices;
                toRemove.Add(oldDecalId);
            }

            foreach (var oldDecalId in toRemove)
            {
                oldChunk.Decals.Remove(oldDecalId);
                oldComp.DecalIndex.Remove(oldDecalId);
            }

            DirtyChunk(ev.Grid, chunkIndices, chunkCollection.GetOrNew(chunkIndices));

            if (oldChunk.Decals.Count == 0)
                oldChunkCollection.Remove(chunkIndices);

            if (toRemove.List?.Count > 0)
                DirtyChunk(ev.OldGrid, chunkIndices, oldChunk);
        }
    }

    protected override void DirtyChunk(EntityUid uid, Vector2i chunkIndices, DecalChunk chunk)
    {
        var id = GetNetEntity(uid);
        chunk.LastModified = Timing.CurTick;
        if(!_dirtyChunks.ContainsKey(id))
            _dirtyChunks[id] = new HashSet<Vector2i>();
        _dirtyChunks[id].Add(chunkIndices);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var ent in _dirtyChunks.Keys)
        {
            if (!TryGetEntity(ent, out var uid))
                continue;

            if (!TryComp(uid, out DecalGridComponent? decals))
                continue;

            Dirty(uid.Value, decals);
        }

        if (!PvsEnabled)
        {
            _dirtyChunks.Clear();
            return;
        }

        if (Sessions.Count > 0)
            _parMan.ProcessNow(_updateJob, Sessions.Count);

        _dirtyChunks.Clear();
    }


    private void ReturnToPool(Dictionary<NetEntity, HashSet<Vector2i>> chunks)
    {
        foreach (var (_, previous) in chunks)
        {
            previous.Clear();
            _chunkIndexPool.Return(previous);
        }

        chunks.Clear();
        _chunkViewerPool.Return(chunks);
    }

    private void SendChunkUpdates(
        ICommonSession session,
        Dictionary<NetEntity, HashSet<Vector2i>> updatedChunks,
        Dictionary<NetEntity, HashSet<Vector2i>> staleChunks)
    {
        var updatedDecals = new Dictionary<NetEntity, Dictionary<Vector2i, DecalChunk>>();
        foreach (var (netGrid, chunks) in updatedChunks)
        {
            var gridId = GetEntity(netGrid);

            if (!TryComp<DecalGridComponent>(gridId, out var comp))
                continue;

            if (comp.ChunkCollection.ChunkCollection == null)
                continue;

            var gridChunks = new Dictionary<Vector2i, DecalChunk>();
            foreach (var indices in chunks)
            {
                gridChunks.Add(indices,
                    comp.ChunkCollection.ChunkCollection.TryGetValue(indices, out var chunk)
                        ? chunk
                        : new());
            }
            updatedDecals[netGrid] = gridChunks;
        }

        if (updatedDecals.Count != 0 || staleChunks.Count != 0)
            RaiseNetworkEvent(new DecalChunkUpdateEvent{Data = updatedDecals, RemovedChunks = staleChunks}, session);

        ReturnToPool(updatedChunks);
        ReturnToPool(staleChunks);
    }

    public void UpdatePlayer(ICommonSession player)
    {
        var chunksInRange = _chunking.GetChunksForSession(player, ChunkSize, _chunkIndexPool, _chunkViewerPool);
        var staleChunks = _chunkViewerPool.Get();
        var previouslySent = PreviousSentChunks[player];

        // Get any chunks not in range anymore
        // Then, remove them from previousSentChunks (for stuff like grids out of range)
        // and also mark them as stale for networking.

        foreach (var (netGrid, oldIndices) in previouslySent)
        {
            // Mark the whole grid as stale and flag for removal.
            if (!chunksInRange.TryGetValue(netGrid, out var chunks))
            {
                previouslySent.Remove(netGrid);

                // Was the grid deleted?
                if (TryGetEntity(netGrid, out var gridId) && HasComp<MapGridComponent>(gridId.Value))
                {
                    // no -> add it to the list of stale chunks
                    staleChunks[netGrid] = oldIndices;
                }
                else
                {
                    // If the grid was deleted then don't worry about telling the client to delete the chunk.
                    oldIndices.Clear();
                    _chunkIndexPool.Return(oldIndices);
                }

                continue;
            }

            var elmo = _chunkIndexPool.Get();

            // Get individual stale chunks.
            foreach (var chunk in oldIndices)
            {
                if (chunks.Contains(chunk))
                    continue;

                elmo.Add(chunk);
            }

            if (elmo.Count == 0)
            {
                _chunkIndexPool.Return(elmo);
                continue;
            }

            staleChunks.Add(netGrid, elmo);
        }

        var updatedChunks = _chunkViewerPool.Get();
        foreach (var (netGrid, gridChunks) in chunksInRange)
        {
            var newChunks = _chunkIndexPool.Get();
            _dirtyChunks.TryGetValue(netGrid, out var dirtyChunks);

            if (!previouslySent.TryGetValue(netGrid, out var previousChunks))
                newChunks.UnionWith(gridChunks);
            else
            {
                foreach (var index in gridChunks)
                {
                    if (!previousChunks.Contains(index) || dirtyChunks != null && dirtyChunks.Contains(index))
                        newChunks.Add(index);
                }

                previousChunks.Clear();
                _chunkIndexPool.Return(previousChunks);
            }

            previouslySent[netGrid] = gridChunks;

            if (newChunks.Count == 0)
                _chunkIndexPool.Return(newChunks);
            else
                updatedChunks[netGrid] = newChunks;
        }

        //send all gridChunks to client
        SendChunkUpdates(player, updatedChunks, staleChunks);
    }

    #region Jobs

    /// <summary>
    /// Updates per-player data for decals.
    /// </summary>
    private record struct UpdatePlayerJob : IParallelRobustJob
    {
        public int BatchSize => 2;

        public DecalSystem System;

        public List<ICommonSession> Sessions;

        public void Execute(int index)
        {
            System.UpdatePlayer(Sessions[index]);
        }
    }

    #endregion
}
