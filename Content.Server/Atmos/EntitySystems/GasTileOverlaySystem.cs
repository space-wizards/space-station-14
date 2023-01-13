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
using Robust.Shared.GameStates;
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

        private bool _pvsEnabled;

        public override void Initialize()
        {
            base.Initialize();
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _confMan.OnValueChanged(CCVars.NetGasOverlayTickRate, UpdateTickRate, true);
            _confMan.OnValueChanged(CCVars.GasOverlayThresholds, UpdateThresholds, true);
            _confMan.OnValueChanged(CVars.NetPVS, OnPvsToggle, true);

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<GasTileOverlayComponent, ComponentGetState>(OnGetState);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
            _confMan.UnsubValueChanged(CCVars.NetGasOverlayTickRate, UpdateTickRate);
            _confMan.UnsubValueChanged(CCVars.GasOverlayThresholds, UpdateThresholds);
            _confMan.UnsubValueChanged(CVars.NetPVS, OnPvsToggle);
        }

        private void OnPvsToggle(bool value)
        {
            if (value == _pvsEnabled)
                return;

            _pvsEnabled = value;

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
            foreach (var (grid, meta) in EntityQuery<GasTileOverlayComponent, MetaDataComponent>(true))
            {
                grid.ForceTick = _gameTiming.CurTick;
                Dirty(grid, meta);
            }
        }

        private void UpdateTickRate(float value) => _updateInterval = value > 0.0f ? 1 / value : float.MaxValue;
        private void UpdateThresholds(int value) => _thresholds = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invalidate(EntityUid grid, Vector2i index, GasTileOverlayComponent? comp = null)
        {
            if (Resolve(grid, ref comp))
                comp.InvalidTiles.Add(index);
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
        ///     Updates the visuals for a tile on some grid chunk. Returns true if the visuals have changed.
        /// </summary>
        private bool UpdateChunkTile(GridAtmosphereComponent gridAtmosphere, GasOverlayChunk chunk, Vector2i index, GameTick curTick)
        {
            ref var oldData = ref chunk.GetData(index);
            if (!gridAtmosphere.Tiles.TryGetValue(index, out var tile))
            {
                if (oldData.Equals(default))
                    return false;

                chunk.LastUpdate = curTick;
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

            chunk.LastUpdate = curTick;
            return true;
        }

        private void UpdateOverlayData(GameTick curTick)
        {
            // TODO parallelize?
            foreach (var (overlay, gam, meta) in EntityQuery<GasTileOverlayComponent, GridAtmosphereComponent, MetaDataComponent>(true))
            {
                bool changed = false;
                foreach (var index in overlay.InvalidTiles)
                {
                    var chunkIndex = GetGasChunkIndices(index);

                    if (!overlay.Chunks.TryGetValue(chunkIndex, out var chunk))
                        overlay.Chunks[chunkIndex] = chunk = new GasOverlayChunk(chunkIndex);

                    changed |= UpdateChunkTile(gam, chunk, index, curTick);
                }

                if (changed)
                    Dirty(overlay, meta);

                overlay.InvalidTiles.Clear();
            }
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

            if (!_pvsEnabled)
                return;

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
                if (!TryComp(grid, out GasTileOverlayComponent? overlay))
                    return;

                List<GasOverlayChunk> dataToSend = new();
                ev.UpdatedChunks[grid] = dataToSend;

                previouslySent.TryGetValue(grid, out var previousChunks);

                foreach (var index in gridChunks)
                {
                    if (!overlay.Chunks.TryGetValue(index, out var value))
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

        private void OnGetState(EntityUid uid, GasTileOverlayComponent component, ref ComponentGetState args)
        {
            if (_pvsEnabled && !args.ReplayState)
                return;

            // Should this be a full component state or a delta-state?
            if (args.FromTick <= component.CreationTick && args.FromTick <= component.ForceTick)
            {
                args.State = new GasTileOverlayState(component.Chunks);
                return;
            }

            var data = new Dictionary<Vector2i, GasOverlayChunk>();
            foreach (var (index, chunk) in component.Chunks)
            {
                if (chunk.LastUpdate >= args.FromTick)
                    data[index] = chunk;
            }

            args.State = new GasTileOverlayState(data) { AllChunks = new(component.Chunks.Keys) };
        }
    }
}
