using System;
using System.Collections.Generic;
using System.Linq;
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
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems.Atmos
{
    [UsedImplicitly]
    internal sealed class GasTileOverlaySystem : SharedGasTileOverlaySystem
    {
        [Robust.Shared.IoC.Dependency] private readonly IGameTiming _gameTiming = default!;
        [Robust.Shared.IoC.Dependency] private readonly IPlayerManager _playerManager = default!;
        [Robust.Shared.IoC.Dependency] private readonly IMapManager _mapManager = default!;
        
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

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _mapManager.OnGridRemoved += OnGridRemoved;
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

        private void OnPlayerStatusChanged(object sender, SessionStatusEventArgs e)
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
            var tileData = new List<GasData>();

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gas = Atmospherics.GetGas(i);
                var overlay = Atmospherics.GetOverlay(i);
                if (overlay == null || tile.Air == null) continue;

                var moles = tile.Air.Gases[i];

                if (moles == 0.0f || moles < gas.GasMolesVisible) continue;

                var data = new GasData(i, MathF.Max(MathF.Min(1, moles / gas.GasMolesVisibleMax), 0f));
                tileData.Add(data);
            }

            overlayData = new GasOverlayData(tile.Hotspot.State, tile.Hotspot.Temperature, tileData.Count == 0 ? null : tileData.ToArray());

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
        private IEnumerable<GasOverlayChunk> GetChunksInRange(IEntity entity)
        {
            var entityTile = _mapManager
                .GetGrid(entity.Transform.GridID)
                .GetTileRef(entity.Transform.GridPosition)
                .GridIndices;

            // This is the max in any direction that we can get a chunk (e.g. max 2 chunks away of data).
            var (maxXDiff, maxYDiff) = ((int) (UpdateRange / ChunkSize) + 1, (int) (UpdateRange / ChunkSize) + 1);
            
            var worldBounds = Box2.CenteredAround(entity.Transform.WorldPosition,
                new Vector2(20.0f, 20.0f));

            foreach (var grid in _mapManager.FindGridsIntersecting(entity.Transform.MapID, worldBounds))
            {
                if (!_overlay.TryGetValue(grid.Index, out var chunks))
                {
                    continue;
                }
                
                for (var x = -maxXDiff; x <= maxXDiff; x++)
                {
                    for (var y = -maxYDiff; y <= maxYDiff; y++)
                    {
                        var chunkIndices = GetGasChunkIndices(new MapIndices(entityTile.X + x * ChunkSize, entityTile.Y + y * ChunkSize));

                        if (!chunks.TryGetValue(chunkIndices, out var chunk)) continue;
                        
                        // Now we'll check if it's in range and relevant for us
                        // (e.g. if we're on the very edge of a chunk we may need more chunks).

                        var (xDiff, yDiff) = (chunkIndices.X - entityTile.X, chunkIndices.Y - entityTile.Y);
                        if (xDiff > 0 && xDiff > UpdateRange ||
                            yDiff > 0 && yDiff > UpdateRange ||
                            xDiff < 0 && Math.Abs(xDiff + ChunkSize) > UpdateRange ||
                            yDiff < 0 && Math.Abs(yDiff + ChunkSize) > UpdateRange) continue;
                        
                        yield return chunk;
                    }
                }
            }
        }

        public override void Update(float frameTime)
        {
            AccumulatedFrameTime += frameTime;

            if (AccumulatedFrameTime < UpdateTime)
            {
                return;
            }
            
            // TODO: So in the worst case scenario we still have to send a LOT of tile data per tick if there's a fire.
            // If we go with say 15 tile radius then we have up to 900 tiles to update per tick.
            // In a saltern fire the worst you'll normally see is around 650 at the moment.
            // Need a way to fake this more because sending almost 2,000 tile updates per second to even 50 players is... yikes
            // I mean that's as big as it gets so larger maps will have the same but still, that's a lot of data.
            
            // Some ways to do this are potentially: splitting fire and gas update data so they don't update at the same time
            // (gives the illusion of more updates happening), e.g. if gas updates are 3 times a second and fires are 1.6 times a second or something.
            // Could also look at updating tiles close to us more frequently (e.g. within 1 chunk every tick).
            // Stuff just out of our viewport we need so when we move it doesn't pop in but it doesn't mean we need to update it every tick.
            
            AccumulatedFrameTime -= UpdateTime;

            var gridAtmosComponents = new Dictionary<GridId, GridAtmosphereComponent>();
            var updatedTiles = new Dictionary<GasOverlayChunk, HashSet<MapIndices>>();
           
            // So up to this point we've been caching the updated tiles for multiple ticks.
            // Now we'll go through and check whether the update actually matters for the overlay or not,
            // and if not then we won't bother sending the data.
            foreach (var (gridId, indices) in _invalidTiles)
            {
                var gridEntityId = _mapManager.GetGrid(gridId).GridEntityId;

                if (!EntityManager.GetEntity(gridEntityId).TryGetComponent(out GridAtmosphereComponent gam))
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

            var currentTime = _gameTiming.CurTime;

            // Set the LastUpdate for chunks.
            foreach (var (chunk, _) in updatedTiles)
            {
                chunk.Dirty(currentTime);
            }

            // Now we'll go through each player, then through each chunk in range of that player checking if the player is still in range
            // If they are, check if they need the new data to send (i.e. if there's an overlay for the gas).
            // Afterwards we reset all the chunk data for the next time we tick.
            foreach (var (session, overlay) in _knownPlayerChunks)
            {
                if (session.AttachedEntity == null) continue;
                
                // Get chunks in range and update if we've moved around or the chunks have new overlay data
                var chunksInRange = GetChunksInRange(session.AttachedEntity).ToArray();
                var knownChunks = overlay.GetKnownChunks().ToArray();
                var chunksToRemove = new List<GasOverlayChunk>(0);
                var chunksToAdd = new List<GasOverlayChunk>(0);
                
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
                    var message = overlay.AddChunk(currentTime, chunk);
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
                    
                    existingData.AddRange(chunk.GetData(invalids));
                }

                foreach (var (grid, data) in clientInvalids)
                {
                    RaiseNetworkEvent(overlay.UpdateClient(grid, data), session.ConnectedClient);
                }
            }

            // Cleanup
            _invalidTiles.Clear();
        }
    }
}
