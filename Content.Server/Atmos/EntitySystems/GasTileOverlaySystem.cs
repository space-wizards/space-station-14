using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.Chunking;
using Content.Shared.GameTicking;
using Content.Shared.Rounding;
using JetBrains.Annotations;
using Microsoft.Extensions.ObjectPool;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

// ReSharper disable once RedundantUsingDirective

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    internal sealed class GasTileOverlaySystem : SharedGasTileOverlaySystem
    {
        [Robust.Shared.IoC.Dependency] private readonly IGameTiming _gameTiming = default!;
        [Robust.Shared.IoC.Dependency] private readonly IPlayerManager _playerManager = default!;
        [Robust.Shared.IoC.Dependency] private readonly IMapManager _mapManager = default!;
        [Robust.Shared.IoC.Dependency] private readonly IConfigurationManager _confMan = default!;
        [Robust.Shared.IoC.Dependency] private readonly IParallelManager _parMan = default!;
        [Robust.Shared.IoC.Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Robust.Shared.IoC.Dependency] private readonly ChunkingSystem _chunkingSys = default!;

        private readonly Dictionary<IPlayerSession, Dictionary<EntityUid, HashSet<Vector2i>>> _lastSentChunks = new();

        /// <summary>
        ///     The tiles that have had their atmos data updated since last tick
        /// </summary>
        private readonly Dictionary<EntityUid, HashSet<Vector2i>> _invalidTiles = new();

        /// <summary>
        ///     Gas data stored in chunks to make PVS / bubbling easier.
        /// </summary>
        private readonly Dictionary<EntityUid, Dictionary<Vector2i, GasOverlayChunk>> _overlay =
            new();

        // Oh look its more duplicated decal system code!
        private ObjectPool<HashSet<Vector2i>> _chunkIndexPool =
            new DefaultObjectPool<HashSet<Vector2i>>(
                new DefaultPooledObjectPolicy<HashSet<Vector2i>>(), 64);
        private ObjectPool<Dictionary<EntityUid, HashSet<Vector2i>>> _chunkViewerPool =
            new DefaultObjectPool<Dictionary<EntityUid, HashSet<Vector2i>>>(
                new DefaultPooledObjectPolicy<Dictionary<EntityUid, HashSet<Vector2i>>>(), 64);

        /// <summary>
        ///     Overlay update interval, in seconds.
        /// </summary>
        private float _updateInterval;

        private int _thresholds;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _confMan.OnValueChanged(CCVars.NetGasOverlayTickRate, UpdateTickRate, true);
            _confMan.OnValueChanged(CCVars.GasOverlayThresholds, UpdateThresholds, true);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
            _confMan.UnsubValueChanged(CCVars.NetGasOverlayTickRate, UpdateTickRate);
            _confMan.UnsubValueChanged(CCVars.GasOverlayThresholds, UpdateThresholds);
        }

        private void UpdateTickRate(float value) => _updateInterval = value > 0.0f ? 1 / value : float.MaxValue;
        private void UpdateThresholds(int value) => _thresholds = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invalidate(EntityUid grid, Vector2i index)
        {
            _invalidTiles.GetOrNew(grid).Add(index);
        }

        private void OnGridRemoved(GridRemovalEvent ev)
        {
            _overlay.Remove(ev.EntityUid);
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

        /// <summary>
        ///     Updates the visuals for a tile on some grid chunk.
        /// </summary>
        private void UpdateChunkTile(GridAtmosphereComponent gridAtmosphere, GasOverlayChunk chunk, Vector2i index, GameTick curTick)
        {
            ref var oldData = ref chunk.GetData(index);
            if (!gridAtmosphere.Tiles.TryGetValue(index, out var tile))
            {
                if (oldData.Equals(default))
                    return;

                chunk.LastUpdate = curTick;
                oldData = default;
                return;
            }

            var changed = oldData.Equals(default) || oldData.FireState != tile.Hotspot.State;
            if (oldData.Equals(default))
                oldData = new GasOverlayData(tile.Hotspot.State, new byte[VisibleGasId.Length]);

            if (tile.Air != null)
            {
                for (var i = 0; i < VisibleGasId.Length; i++)
                {
                    var id = VisibleGasId[i];
                    var gas = _atmosphereSystem.GetGas(id);
                    var moles = tile.Air.Moles[id];
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

                    var opacity = (byte) (ContentHelpers.RoundToLevels(
                        MathHelper.Clamp01((moles - gas.GasMolesVisible) /
                                           (gas.GasMolesVisibleMax - gas.GasMolesVisible)) * 255, byte.MaxValue,
                        _thresholds) * 255 / (_thresholds - 1));

                    if (oldOpacity == opacity)
                        continue;

                    oldOpacity = opacity;
                    changed = true;
                }
            }

            if (!changed)
                return;

            chunk.LastUpdate = curTick;
        }

        private void UpdateOverlayData(GameTick curTick)
        {
            // TODO parallelize?
            foreach (var (gridId, invalidIndices) in _invalidTiles)
            {
                if (!TryComp(gridId, out GridAtmosphereComponent? gam))
                {
                    _overlay.Remove(gridId);
                    continue;
                }

                var chunks = _overlay.GetOrNew(gridId);

                foreach (var index in invalidIndices)
                {
                    var chunkIndex = GetGasChunkIndices(index);

                    if (!chunks.TryGetValue(chunkIndex, out var chunk))
                        chunks[chunkIndex] = chunk = new GasOverlayChunk(chunkIndex);

                    UpdateChunkTile(gam, chunk, index, curTick);
                }
            }
            _invalidTiles.Clear();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            AccumulatedFrameTime += frameTime;

            if (AccumulatedFrameTime < _updateInterval) return;
            AccumulatedFrameTime -= _updateInterval;

            var curTick = _gameTiming.CurTick;

            // First, update per-chunk visual data for any invalidated tiles.
            UpdateOverlayData(curTick);

            // Now we'll go through each player, then through each chunk in range of that player checking if the player is still in range
            // If they are, check if they need the new data to send (i.e. if there's an overlay for the gas).
            // Afterwards we reset all the chunk data for the next time we tick.
            var players = _playerManager.ServerSessions.Where(x => x.Status == SessionStatus.InGame).ToArray();
            var opts = new ParallelOptions { MaxDegreeOfParallelism = _parMan.ParallelProcessCount };
            Parallel.ForEach(players, opts, p => UpdatePlayer(p, curTick));
        }

        private void UpdatePlayer(IPlayerSession playerSession, GameTick curTick)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            var chunksInRange = _chunkingSys.GetChunksForSession(playerSession, ChunkSize, xformQuery, _chunkIndexPool, _chunkViewerPool);
            var previouslySent = _lastSentChunks[playerSession];

            var ev = new GasOverlayUpdateEvent();

            foreach (var (grid, oldIndices) in previouslySent)
            {
                // Mark the whole grid as stale and flag for removal.
                if (!chunksInRange.TryGetValue(grid, out var chunks))
                {
                    previouslySent.Remove(grid);

                    // If grid was deleted then don't worry about sending it to the client.
                    if (_mapManager.IsGrid(grid))
                        ev.RemovedChunks[grid] = oldIndices;
                    else
                    {
                        oldIndices.Clear();
                        _chunkIndexPool.Return(oldIndices);
                    }

                    continue;
                }

                var old = _chunkIndexPool.Get();
                DebugTools.Assert(old.Count == 0);
                foreach (var chunk in oldIndices)
                {
                    if (!chunks.Contains(chunk))
                        old.Add(chunk);
                }

                if (old.Count == 0)
                    _chunkIndexPool.Return(old);
                else
                    ev.RemovedChunks.Add(grid, old);
            }

            foreach (var (grid, gridChunks) in chunksInRange)
            {
                // Not all grids have atmospheres.
                if (!_overlay.TryGetValue(grid, out var gridData))
                    continue;

                List<GasOverlayChunk> dataToSend = new();
                ev.UpdatedChunks[grid] = dataToSend;

                previouslySent.TryGetValue(grid, out var previousChunks);

                foreach (var index in gridChunks)
                {
                    if (!gridData.TryGetValue(index, out var value))
                        continue;

                    if (previousChunks != null &&
                        previousChunks.Contains(index) &&
                        value.LastUpdate != curTick)
                        continue;

                    dataToSend.Add(value);
                }

                previouslySent[grid] = gridChunks;
                if (previousChunks != null)
                {
                    previousChunks.Clear();
                    _chunkIndexPool.Return(previousChunks);
                }
            }

            if (ev.UpdatedChunks.Count != 0 || ev.RemovedChunks.Count != 0)
                RaiseNetworkEvent(ev, playerSession.ConnectedClient);
        }

        public override void Reset(RoundRestartCleanupEvent ev)
        {
            _invalidTiles.Clear();
            _overlay.Clear();

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
    }
}
