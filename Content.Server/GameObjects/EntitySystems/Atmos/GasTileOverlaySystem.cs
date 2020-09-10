#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Content.Server.GameObjects.Components.Atmos;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.EntitySystems.Atmos;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Server.GameObjects.EntitySystems.Atmos
{
    [UsedImplicitly]
    internal sealed class GasTileOverlaySystem : SharedGasTileOverlaySystem
    {
        [Robust.Shared.IoC.Dependency] private readonly IGameTiming _gameTiming = default!;
        [Robust.Shared.IoC.Dependency] private readonly IPlayerManager _playerManager = default!;
        [Robust.Shared.IoC.Dependency] private readonly IMapManager _mapManager = default!;
        [Robust.Shared.IoC.Dependency] private readonly IConfigurationManager _configManager = default!;

        /// <summary>
        ///     The tiles that have had their atmos data updated since last tick
        /// </summary>
        private Dictionary<GridId, HashSet<MapIndices>> _invalidTiles = new Dictionary<GridId, HashSet<MapIndices>>();

        private Dictionary<IPlayerSession, PlayerGasOverlay> _knownPlayerChunks =
            new Dictionary<IPlayerSession, PlayerGasOverlay>();

        /// <summary>
        ///     Gas data stored in chunks to make PVS / bubbling easier.
        /// </summary>
        private Dictionary<GridId, Dictionary<MapIndices, GasOverlayChunk>> _overlay =
            new Dictionary<GridId, Dictionary<MapIndices, GasOverlayChunk>>();

        /// <summary>
        ///     How far away do we update gas overlays (minimum; due to chunking further away tiles may also be updated).
        /// </summary>
        private float _updateRange;
        // Because the gas overlay updates aren't run every tick we need to avoid the pop-in that might occur with
        // the regular PVS range.
        private const float RangeOffset = 6.0f;

        /// <summary>
        ///     Overlay update ticks per second.
        /// </summary>
        private float _updateCooldown;

        private AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            _atmosphereSystem = Get<AtmosphereSystem>();
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _mapManager.OnGridRemoved += OnGridRemoved;
            _configManager.RegisterCVar("net.gasoverlaytickrate", 3.0f);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
            _mapManager.OnGridRemoved -= OnGridRemoved;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invalidate(GridId gridIndex, MapIndices indices)
        {
            if (!_invalidTiles.TryGetValue(gridIndex, out var existing))
            {
                existing = new HashSet<MapIndices>();
                _invalidTiles[gridIndex] = existing;
            }

            existing.Add(indices);
        }

        private GasOverlayChunk GetOrCreateChunk(GridId gridIndex, MapIndices indices)
        {
            if (!_overlay.TryGetValue(gridIndex, out var chunks))
            {
                chunks = new Dictionary<MapIndices, GasOverlayChunk>();
                _overlay[gridIndex] = chunks;
            }

            var chunkIndices = GetGasChunkIndices(indices);

            if (!chunks.TryGetValue(chunkIndices, out var chunk))
            {
                chunk = new GasOverlayChunk(gridIndex, chunkIndices);
                chunks[chunkIndices] = chunk;
            }

            return chunk;
        }

        private void OnGridRemoved(GridId gridId)
        {
            if (_overlay.ContainsKey(gridId))
            {
                _overlay.Remove(gridId);
            }
        }

        public void ResettingCleanup()
        {
            _invalidTiles.Clear();
            _overlay.Clear();

            foreach (var (_, data) in _knownPlayerChunks)
            {
                data.Reset();
            }
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.InGame)
            {
                if (_knownPlayerChunks.ContainsKey(e.Session))
                {
                    _knownPlayerChunks.Remove(e.Session);
                }

                return;
            }

            if (!_knownPlayerChunks.ContainsKey(e.Session))
            {
                _knownPlayerChunks[e.Session] = new PlayerGasOverlay();
            }
        }

        /// <summary>
        ///     Checks whether the overlay-relevant data for a gas tile has been updated.
        /// </summary>
        /// <param name="gam"></param>
        /// <param name="oldTile"></param>
        /// <param name="indices"></param>
        /// <param name="overlayData"></param>
        /// <returns>true if updated</returns>
        private bool TryRefreshTile(GridAtmosphereComponent gam, GasOverlayData oldTile, MapIndices indices, out GasOverlayData overlayData)
        {
            var tile = gam.GetTile(indices);

            if (tile == null)
            {
                overlayData = default;
                return false;
            }

            var tileData = new List<GasData>();

            for (byte i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gas = _atmosphereSystem.GetGas(i);
                var overlay = _atmosphereSystem.GetOverlay(i);
                if (overlay == null || tile?.Air == null) continue;

                var moles = tile.Air.Gases[i];

                if (moles < gas.GasMolesVisible) continue;

                var data = new GasData(i, (byte) (MathHelper.Clamp01(moles / gas.GasMolesVisibleMax) * 255));
                tileData.Add(data);
            }

            overlayData = new GasOverlayData(tile!.Hotspot.State, tile.Hotspot.Temperature, tileData.Count == 0 ? null : tileData.ToArray());

            if (overlayData.Equals(oldTile))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Get every chunk in range of our entity that exists, including on other grids.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private List<GasOverlayChunk> GetChunksInRange(IEntity entity)
        {
            var inRange = new List<GasOverlayChunk>();

            // This is the max in any direction that we can get a chunk (e.g. max 2 chunks away of data).
            var (maxXDiff, maxYDiff) = ((int) (_updateRange / ChunkSize) + 1, (int) (_updateRange / ChunkSize) + 1);

            var worldBounds = Box2.CenteredAround(entity.Transform.WorldPosition,
                new Vector2(_updateRange, _updateRange));

            foreach (var grid in _mapManager.FindGridsIntersecting(entity.Transform.MapID, worldBounds))
            {
                if (!_overlay.TryGetValue(grid.Index, out var chunks))
                {
                    continue;
                }

                var entityTile = grid.GetTileRef(entity.Transform.Coordinates).GridIndices;

                for (var x = -maxXDiff; x <= maxXDiff; x++)
                {
                    for (var y = -maxYDiff; y <= maxYDiff; y++)
                    {
                        var chunkIndices = GetGasChunkIndices(new MapIndices(entityTile.X + x * ChunkSize, entityTile.Y + y * ChunkSize));

                        if (!chunks.TryGetValue(chunkIndices, out var chunk)) continue;

                        // Now we'll check if it's in range and relevant for us
                        // (e.g. if we're on the very edge of a chunk we may need more chunks).

                        var (xDiff, yDiff) = (chunkIndices.X - entityTile.X, chunkIndices.Y - entityTile.Y);
                        if (xDiff > 0 && xDiff > _updateRange ||
                            yDiff > 0 && yDiff > _updateRange ||
                            xDiff < 0 && Math.Abs(xDiff + ChunkSize) > _updateRange ||
                            yDiff < 0 && Math.Abs(yDiff + ChunkSize) > _updateRange) continue;

                        inRange.Add(chunk);
                    }
                }
            }

            return inRange;
        }

        public override void Update(float frameTime)
        {
            AccumulatedFrameTime += frameTime;
            _updateCooldown = 1 / _configManager.GetCVar<float>("net.gasoverlaytickrate");

            if (AccumulatedFrameTime < _updateCooldown)
            {
                return;
            }

            _updateRange = _configManager.GetCVar<float>("net.maxupdaterange") + RangeOffset;

            // TODO: So in the worst case scenario we still have to send a LOT of tile data per tick if there's a fire.
            // If we go with say 15 tile radius then we have up to 900 tiles to update per tick.
            // In a saltern fire the worst you'll normally see is around 650 at the moment.
            // Need a way to fake this more because sending almost 2,000 tile updates per second to even 50 players is... yikes
            // I mean that's as big as it gets so larger maps will have the same but still, that's a lot of data.

            // Some ways to do this are potentially: splitting fire and gas update data so they don't update at the same time
            // (gives the illusion of more updates happening), e.g. if gas updates are 3 times a second and fires are 1.6 times a second or something.
            // Could also look at updating tiles close to us more frequently (e.g. within 1 chunk every tick).
            // Stuff just out of our viewport we need so when we move it doesn't pop in but it doesn't mean we need to update it every tick.

            AccumulatedFrameTime -= _updateCooldown;

            var gridAtmosComponents = new Dictionary<GridId, GridAtmosphereComponent>();
            var updatedTiles = new Dictionary<GasOverlayChunk, HashSet<MapIndices>>();

            // So up to this point we've been caching the updated tiles for multiple ticks.
            // Now we'll go through and check whether the update actually matters for the overlay or not,
            // and if not then we won't bother sending the data.
            foreach (var (gridId, indices) in _invalidTiles)
            {
                if (!_mapManager.TryGetGrid(gridId, out var grid))
                {
                    return;
                }

                var gridEntityId = grid.GridEntityId;

                if (!EntityManager.GetEntity(gridEntityId).TryGetComponent(out GridAtmosphereComponent? gam))
                {
                    continue;
                }

                // If it's being invalidated it should have this right?
                // At any rate we'll cache it for here + the AddChunk
                if (!gridAtmosComponents.ContainsKey(gridId))
                {
                    gridAtmosComponents[gridId] = gam;
                }

                foreach (var invalid in indices)
                {
                    var chunk = GetOrCreateChunk(gridId, invalid);

                    if (!TryRefreshTile(gam, chunk.GetData(invalid), invalid, out var data)) continue;

                    if (!updatedTiles.TryGetValue(chunk, out var tiles))
                    {
                        tiles = new HashSet<MapIndices>();
                        updatedTiles[chunk] = tiles;
                    }

                    updatedTiles[chunk].Add(invalid);
                    chunk.Update(data, invalid);
                }
            }

            var currentTick = _gameTiming.CurTick;

            // Set the LastUpdate for chunks.
            foreach (var (chunk, _) in updatedTiles)
            {
                chunk.Dirty(currentTick);
            }

            // Now we'll go through each player, then through each chunk in range of that player checking if the player is still in range
            // If they are, check if they need the new data to send (i.e. if there's an overlay for the gas).
            // Afterwards we reset all the chunk data for the next time we tick.
            foreach (var (session, overlay) in _knownPlayerChunks)
            {
                if (session.AttachedEntity == null) continue;

                // Get chunks in range and update if we've moved around or the chunks have new overlay data
                var chunksInRange = GetChunksInRange(session.AttachedEntity);
                var knownChunks = overlay.GetKnownChunks();
                var chunksToRemove = new List<GasOverlayChunk>();
                var chunksToAdd = new List<GasOverlayChunk>();

                foreach (var chunk in chunksInRange)
                {
                    if (!knownChunks.Contains(chunk))
                    {
                        chunksToAdd.Add(chunk);
                    }
                }

                foreach (var chunk in knownChunks)
                {
                    if (!chunksInRange.Contains(chunk))
                    {
                        chunksToRemove.Add(chunk);
                    }
                }

                foreach (var chunk in chunksToAdd)
                {
                    var message = overlay.AddChunk(currentTick, chunk);
                    if (message != null)
                    {
                        RaiseNetworkEvent(message, session.ConnectedClient);
                    }
                }

                foreach (var chunk in chunksToRemove)
                {
                    overlay.RemoveChunk(chunk);
                }

                var clientInvalids = new Dictionary<GridId, List<(MapIndices, GasOverlayData)>>();

                // Check for any dirty chunks in range and bundle the data to send to the client.
                foreach (var chunk in chunksInRange)
                {
                    if (!updatedTiles.TryGetValue(chunk, out var invalids)) continue;

                    if (!clientInvalids.TryGetValue(chunk.GridIndices, out var existingData))
                    {
                        existingData = new List<(MapIndices, GasOverlayData)>();
                        clientInvalids[chunk.GridIndices] = existingData;
                    }

                    chunk.GetData(existingData, invalids);
                }

                foreach (var (grid, data) in clientInvalids)
                {
                    RaiseNetworkEvent(overlay.UpdateClient(grid, data), session.ConnectedClient);
                }
            }

            // Cleanup
            _invalidTiles.Clear();
        }
        private sealed class PlayerGasOverlay
        {
            private readonly Dictionary<GridId, Dictionary<MapIndices, GasOverlayChunk>> _data =
                new Dictionary<GridId, Dictionary<MapIndices, GasOverlayChunk>>();

            private readonly Dictionary<GasOverlayChunk, GameTick> _lastSent =
                new Dictionary<GasOverlayChunk, GameTick>();

            public GasOverlayMessage UpdateClient(GridId grid, List<(MapIndices, GasOverlayData)> data)
            {
                return new GasOverlayMessage(grid, data);
            }

            public void Reset()
            {
                _data.Clear();
                _lastSent.Clear();
            }

            public List<GasOverlayChunk> GetKnownChunks()
            {
                var known = new List<GasOverlayChunk>();

                foreach (var (_, chunks) in _data)
                {
                    foreach (var (_, chunk) in chunks)
                    {
                        known.Add(chunk);
                    }
                }

                return known;
            }

            public GasOverlayMessage? AddChunk(GameTick currentTick, GasOverlayChunk chunk)
            {
                if (!_data.TryGetValue(chunk.GridIndices, out var chunks))
                {
                    chunks = new Dictionary<MapIndices, GasOverlayChunk>();
                    _data[chunk.GridIndices] = chunks;
                }

                if (_lastSent.TryGetValue(chunk, out var last) && last >= chunk.LastUpdate)
                {
                    return null;
                }

                _lastSent[chunk] = currentTick;
                var message = ChunkToMessage(chunk);

                return message;
            }

            public void RemoveChunk(GasOverlayChunk chunk)
            {
                // Don't need to sync to client as they can manage it themself.
                if (!_data.TryGetValue(chunk.GridIndices, out var chunks))
                {
                    return;
                }

                if (chunks.ContainsKey(chunk.MapIndices))
                {
                    chunks.Remove(chunk.MapIndices);
                }
            }

            /// <summary>
            ///     Retrieve a whole chunk as a message, only getting the relevant tiles for the gas overlay.
            /// </summary>
            /// <param name="chunk"></param>
            /// <returns></returns>
            private GasOverlayMessage? ChunkToMessage(GasOverlayChunk chunk)
            {
                // Chunk data should already be up to date.
                // Only send relevant tiles to client.

                var tileData = new List<(MapIndices, GasOverlayData)>();

                for (var x = 0; x < ChunkSize; x++)
                {
                    for (var y = 0; y < ChunkSize; y++)
                    {
                        // TODO: Check could be more robust I think.
                        var data = chunk.TileData[x, y];
                        if ((data.Gas == null || data.Gas.Length == 0) && data.FireState == 0 && data.FireTemperature == 0.0f)
                        {
                            continue;
                        }

                        var indices = new MapIndices(chunk.MapIndices.X + x, chunk.MapIndices.Y + y);
                        tileData.Add((indices, data));
                    }
                }

                if (tileData.Count == 0)
                {
                    return null;
                }

                return new GasOverlayMessage(chunk.GridIndices, tileData);
            }
        }
    }
}
