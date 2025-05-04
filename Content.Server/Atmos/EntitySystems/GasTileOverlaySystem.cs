using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.Chunking;
using Content.Shared.GameTicking;
using Content.Shared.Rounding;
using JetBrains.Annotations;
using Microsoft.Extensions.ObjectPool;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

// ReSharper disable once RedundantUsingDirective

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasTileOverlaySystem : SharedGasTileOverlaySystem
    {
        [Robust.Shared.IoC.Dependency] private readonly IGameTiming _gameTiming = default!;
        [Robust.Shared.IoC.Dependency] private readonly IPlayerManager _playerManager = default!;
        [Robust.Shared.IoC.Dependency] private readonly IMapManager _mapManager = default!;
        [Robust.Shared.IoC.Dependency] private readonly IConfigurationManager _confMan = default!;
        [Robust.Shared.IoC.Dependency] private readonly IParallelManager _parMan = default!;
        [Robust.Shared.IoC.Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Robust.Shared.IoC.Dependency] private readonly ChunkingSystem _chunkingSys = default!;

        /// <summary>
        /// Per-tick cache of sessions.
        /// </summary>
        private readonly List<ICommonSession> _sessions = new();
        private UpdatePlayerJob _updateJob;

        private readonly Dictionary<ICommonSession, Dictionary<NetEntity, HashSet<Vector2i>>> _lastSentChunks = new();

        // Oh look its more duplicated decal system code!
        private ObjectPool<HashSet<Vector2i>> _chunkIndexPool =
            new DefaultObjectPool<HashSet<Vector2i>>(
                new DefaultPooledObjectPolicy<HashSet<Vector2i>>(), 64);
        private ObjectPool<Dictionary<NetEntity, HashSet<Vector2i>>> _chunkViewerPool =
            new DefaultObjectPool<Dictionary<NetEntity, HashSet<Vector2i>>>(
                new DefaultPooledObjectPolicy<Dictionary<NetEntity, HashSet<Vector2i>>>(), 64);

        private bool _doSessionUpdate;

        /// <summary>
        ///     Overlay update interval, in seconds.
        /// </summary>
        private float _updateInterval;

        private int _thresholds;
        private EntityQuery<MapGridComponent> _gridQuery;
        private EntityQuery<GasTileOverlayComponent> _query;

        public override void Initialize()
        {
            base.Initialize();

            _query = GetEntityQuery<GasTileOverlayComponent>();
            _gridQuery = GetEntityQuery<MapGridComponent>();

            _updateJob = new UpdatePlayerJob()
            {
                EntManager = EntityManager,
                System = this,
                ChunkIndexPool = _chunkIndexPool,
                Sessions = _sessions,
                ChunkingSys = _chunkingSys,
                MapManager = _mapManager,
                ChunkViewerPool = _chunkViewerPool,
                LastSentChunks = _lastSentChunks,
                GridQuery = _gridQuery,
            };

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            Subs.CVar(_confMan, CCVars.NetGasOverlayTickRate, UpdateTickRate, true);
            Subs.CVar(_confMan, CCVars.GasOverlayThresholds, UpdateThresholds, true);
            Subs.CVar(_confMan, CVars.NetPVS, OnPvsToggle, true);

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<GasTileOverlayComponent, ComponentStartup>(OnStartup);
        }

        private void OnStartup(EntityUid uid, GasTileOverlayComponent component, ComponentStartup args)
        {
            // This **shouldn't** be required, but just in case we ever get entity prototypes that have gas overlays, we
            // need to ensure that we send an initial full state to players.
            Dirty(uid, component);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        }

        private void OnPvsToggle(bool value)
        {
            if (value == PvsEnabled)
                return;

            PvsEnabled = value;

            if (value)
                return;

            foreach (var lastSent in _lastSentChunks.Values)
            {
                foreach (var set in lastSent.Values)
                {
                    set.Clear();
                    _chunkIndexPool.Return(set);
                }
                lastSent.Clear();
            }

            // PVS was turned off, ensure data gets sent to all clients.
            var query = AllEntityQuery<GasTileOverlayComponent, MetaDataComponent>();
            while (query.MoveNext(out var uid, out var grid, out var meta))
            {
                grid.ForceTick = _gameTiming.CurTick;
                Dirty(uid, grid, meta);
            }
        }

        private void UpdateTickRate(float value) => _updateInterval = value > 0.0f ? 1 / value : float.MaxValue;
        private void UpdateThresholds(int value) => _thresholds = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invalidate(Entity<GasTileOverlayComponent?> grid, Vector2i index)
        {
            if (_query.Resolve(grid.Owner, ref grid.Comp))
                grid.Comp.InvalidTiles.Add(index);
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.InGame)
            {
                if (_lastSentChunks.Remove(e.Session, out var sets))
                {
                    foreach (var set in sets.Values)
                    {
                        set.Clear();
                        _chunkIndexPool.Return(set);
                    }
                }
            }

            if (!_lastSentChunks.ContainsKey(e.Session))
            {
                _lastSentChunks[e.Session] = new();
            }
        }

        private byte GetOpacity(float moles, float molesVisible, float molesVisibleMax)
        {
            return (byte) (ContentHelpers.RoundToLevels(
                MathHelper.Clamp01((moles - molesVisible) /
                                   (molesVisibleMax - molesVisible)) * 255, byte.MaxValue,
                _thresholds) * 255 / (_thresholds - 1));
        }

        public GasOverlayData GetOverlayData(GasMixture? mixture)
        {
            var data = new GasOverlayData(0, new byte[VisibleGasId.Length]);

            for (var i = 0; i < VisibleGasId.Length; i++)
            {
                var id = VisibleGasId[i];
                var gas = _atmosphereSystem.GetGas(id);
                var moles = mixture?[id] ?? 0f;
                ref var opacity = ref data.Opacity[i];

                if (moles < gas.GasMolesVisible)
                {
                    continue;
                }

                opacity = (byte) (ContentHelpers.RoundToLevels(
                    MathHelper.Clamp01((moles - gas.GasMolesVisible) /
                                       (gas.GasMolesVisibleMax - gas.GasMolesVisible)) * 255, byte.MaxValue,
                    _thresholds) * 255 / (_thresholds - 1));
            }

            return data;
        }

        /// <summary>
        ///     Updates the visuals for a tile on some grid chunk. Returns true if the visuals have changed.
        /// </summary>
        private bool UpdateChunkTile(GridAtmosphereComponent gridAtmosphere, GasOverlayChunk chunk, Vector2i index)
        {
            ref var oldData = ref chunk.TileData[chunk.GetDataIndex(index)];
            if (!gridAtmosphere.Tiles.TryGetValue(index, out var tile))
            {
                if (oldData.Equals(default))
                    return false;

                chunk.LastUpdate = _gameTiming.CurTick;
                oldData = default;
                return true;
            }

            var changed = false;
            if (oldData.Equals(default))
            {
                changed = true;
                oldData = new GasOverlayData(tile.Hotspot.State, new byte[VisibleGasId.Length]);
            }
            else if (oldData.FireState != tile.Hotspot.State)
            {
                changed = true;
                oldData = new GasOverlayData(tile.Hotspot.State, oldData.Opacity);
            }

            if (tile is {Air: not null, NoGridTile: false})
            {
                for (var i = 0; i < VisibleGasId.Length; i++)
                {
                    var id = VisibleGasId[i];
                    var gas = _atmosphereSystem.GetGas(id);
                    var moles = tile.Air[id];
                    ref var oldOpacity = ref oldData.Opacity[i];

                    if (moles < gas.GasMolesVisible)
                    {
                        if (oldOpacity != 0)
                        {
                            oldOpacity = 0;
                            changed = true;
                        }

                        continue;
                    }

                    var opacity = GetOpacity(moles, gas.GasMolesVisible, gas.GasMolesVisibleMax);

                    if (oldOpacity == opacity)
                        continue;

                    oldOpacity = opacity;
                    changed = true;
                }
            }
            else
            {
                for (var i = 0; i < VisibleGasId.Length; i++)
                {
                    changed |= oldData.Opacity[i] != 0;
                    oldData.Opacity[i] = 0;
                }
            }

            if (!changed)
                return false;

            chunk.LastUpdate = _gameTiming.CurTick;
            return true;
        }

        private void UpdateOverlayData()
        {
            // TODO parallelize?
            var query = AllEntityQuery<GasTileOverlayComponent, GridAtmosphereComponent, MetaDataComponent>();
            while (query.MoveNext(out var uid, out var overlay, out var gam, out var meta))
            {
                var changed = false;
                foreach (var index in overlay.InvalidTiles)
                {
                    var chunkIndex = GetGasChunkIndices(index);

                    if (!overlay.Chunks.TryGetValue(chunkIndex, out var chunk))
                        overlay.Chunks[chunkIndex] = chunk = new GasOverlayChunk(chunkIndex);

                    changed |= UpdateChunkTile(gam, chunk, index);
                }

                if (changed)
                    Dirty(uid, overlay, meta);

                overlay.InvalidTiles.Clear();
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            AccumulatedFrameTime += frameTime;

            if (_doSessionUpdate)
            {
                UpdateSessions();
                return;
            }

            if (AccumulatedFrameTime < _updateInterval)
                return;

            AccumulatedFrameTime -= _updateInterval;

            // First, update per-chunk visual data for any invalidated tiles.
            UpdateOverlayData();

            // Then, next tick we send the data to players.
            // This is to avoid doing all the work in the same tick.
            _doSessionUpdate = true;
        }

        public void UpdateSessions()
        {
            _doSessionUpdate = false;

            if (!PvsEnabled)
                return;

            // Now we'll go through each player, then through each chunk in range of that player checking if the player is still in range
            // If they are, check if they need the new data to send (i.e. if there's an overlay for the gas).
            // Afterwards we reset all the chunk data for the next time we tick.
            _sessions.Clear();

            foreach (var player in _playerManager.Sessions)
            {
                if (player.Status != SessionStatus.InGame)
                    continue;

                _sessions.Add(player);
            }

            if (_sessions.Count == 0)
                return;

            _parMan.ProcessNow(_updateJob, _sessions.Count);
            _updateJob.LastSessionUpdate = _gameTiming.CurTick;
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            foreach (var data in _lastSentChunks.Values)
            {
                foreach (var previous in data.Values)
                {
                    previous.Clear();
                    _chunkIndexPool.Return(previous);
                }

                data.Clear();
            }
        }

        #region Jobs

        /// <summary>
        /// Updates per player gas overlay data.
        /// </summary>
        private record struct UpdatePlayerJob : IParallelRobustJob
        {
            public int BatchSize => 2;

            public IEntityManager EntManager;
            public IMapManager MapManager;
            public ChunkingSystem ChunkingSys;
            public GasTileOverlaySystem System;
            public ObjectPool<HashSet<Vector2i>> ChunkIndexPool;
            public ObjectPool<Dictionary<NetEntity, HashSet<Vector2i>>> ChunkViewerPool;

            public GameTick LastSessionUpdate;
            public Dictionary<ICommonSession, Dictionary<NetEntity, HashSet<Vector2i>>> LastSentChunks;
            public List<ICommonSession> Sessions;

            public EntityQuery<MapGridComponent> GridQuery;

            public void Execute(int index)
            {
                var playerSession = Sessions[index];
                var chunksInRange = ChunkingSys.GetChunksForSession(playerSession, ChunkSize, ChunkIndexPool, ChunkViewerPool);
                var previouslySent = LastSentChunks[playerSession];

                var ev = new GasOverlayUpdateEvent();

                foreach (var (netGrid, oldIndices) in previouslySent)
                {
                    // Mark the whole grid as stale and flag for removal.
                    if (!chunksInRange.TryGetValue(netGrid, out var chunks))
                    {
                        previouslySent.Remove(netGrid);

                        // If grid was deleted then don't worry about sending it to the client.
                        if (!EntManager.TryGetEntity(netGrid, out var gridId) || GridQuery.HasComp(gridId.Value))
                            ev.RemovedChunks[netGrid] = oldIndices;
                        else
                        {
                            oldIndices.Clear();
                            ChunkIndexPool.Return(oldIndices);
                        }

                        continue;
                    }

                    var old = ChunkIndexPool.Get();
                    DebugTools.Assert(old.Count == 0);
                    foreach (var chunk in oldIndices)
                    {
                        if (!chunks.Contains(chunk))
                            old.Add(chunk);
                    }

                    if (old.Count == 0)
                        ChunkIndexPool.Return(old);
                    else
                        ev.RemovedChunks.Add(netGrid, old);
                }

                foreach (var (netGrid, gridChunks) in chunksInRange)
                {
                    // Not all grids have atmospheres.
                    if (!EntManager.TryGetEntity(netGrid, out var grid) || !EntManager.TryGetComponent(grid, out GasTileOverlayComponent? overlay))
                        continue;

                    List<GasOverlayChunk> dataToSend = new();
                    ev.UpdatedChunks[netGrid] = dataToSend;

                    previouslySent.TryGetValue(netGrid, out var previousChunks);

                    foreach (var gIndex in gridChunks)
                    {
                        if (!overlay.Chunks.TryGetValue(gIndex, out var value))
                            continue;

                        // If the chunk was updated since we last sent it, send it again
                        if (value.LastUpdate > LastSessionUpdate)
                        {
                            dataToSend.Add(value);
                            continue;
                        }

                        // Always send it if we didn't previously send it
                        if (previousChunks == null || !previousChunks.Contains(gIndex))
                            dataToSend.Add(value);
                    }

                    previouslySent[netGrid] = gridChunks;
                    if (previousChunks != null)
                    {
                        previousChunks.Clear();
                        ChunkIndexPool.Return(previousChunks);
                    }
                }

                if (ev.UpdatedChunks.Count != 0 || ev.RemovedChunks.Count != 0)
                    System.RaiseNetworkEvent(ev, playerSession.Channel);
            }
        }

        #endregion
    }
}
