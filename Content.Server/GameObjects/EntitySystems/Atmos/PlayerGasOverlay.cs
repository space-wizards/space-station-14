using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.EntitySystems.Atmos;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems.Atmos
{
    public sealed class PlayerGasOverlay
    {
        private readonly Dictionary<GridId, Dictionary<MapIndices, GasOverlayChunk>> _data = 
            new Dictionary<GridId, Dictionary<MapIndices, GasOverlayChunk>>();
        
        private readonly Dictionary<GasOverlayChunk, TimeSpan> _lastSent = 
            new Dictionary<GasOverlayChunk, TimeSpan>();

        public SharedGasTileOverlaySystem.GasOverlayMessage UpdateClient(GridId grid, List<(MapIndices, SharedGasTileOverlaySystem.GasOverlayData)> data)
        {
            return new SharedGasTileOverlaySystem.GasOverlayMessage(grid, data);
        }

        public void Reset()
        {
            _data.Clear();
            _lastSent.Clear();
        }
        
        public IEnumerable<GasOverlayChunk> GetKnownChunks()
        {
            foreach (var (_, chunks) in _data)
            {
                foreach (var (_, chunk) in chunks)
                {
                    yield return chunk;
                }
            }
        }

        public SharedGasTileOverlaySystem.GasOverlayMessage AddChunk(TimeSpan currentTime, GasOverlayChunk chunk)
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
            
            _lastSent[chunk] = currentTime;
            TryChunkToMessage(chunk, out var message);

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
        ///     Don't use this for UpdateClient as it won't account for tiles that had an overlay texture that's been removed.
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool TryChunkToMessage(GasOverlayChunk chunk, [NotNullWhen(true)] out SharedGasTileOverlaySystem.GasOverlayMessage? message)
        {
            // Chunk data should already be up to date.
            // Only send relevant tiles to client.
            
            var tileData = new List<(MapIndices, SharedGasTileOverlaySystem.GasOverlayData)>();

            for (var x = 0; x < SharedGasTileOverlaySystem.ChunkSize; x++)
            {
                for (var y = 0; y < SharedGasTileOverlaySystem.ChunkSize; y++)
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
                message = null;
                return false;
            }
            
            message = new SharedGasTileOverlaySystem.GasOverlayMessage(chunk.GridIndices, tileData);
            return true;
        }
    }
}