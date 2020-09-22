using System.Collections.Generic;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.GameObjects.EntitySystems.Atmos
{
    public sealed class GasOverlayChunk
    {
        /// <summary>
        ///     Grid for this chunk
        /// </summary>
        public GridId GridIndices { get; }
        
        /// <summary>
        ///     Origin of this chunk
        /// </summary>
        public MapIndices MapIndices { get; }
        
        public SharedGasTileOverlaySystem.GasOverlayData[,] TileData = new SharedGasTileOverlaySystem.GasOverlayData[SharedGasTileOverlaySystem.ChunkSize, SharedGasTileOverlaySystem.ChunkSize];

        public GameTick LastUpdate { get; private set; }

        public GasOverlayChunk(GridId gridIndices, MapIndices mapIndices)
        {
            GridIndices = gridIndices;
            MapIndices = mapIndices;
        }

        public void Dirty(GameTick currentTick)
        {
            LastUpdate = currentTick;
        }

        /// <summary>
        ///     Flags Dirty if the data is different.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="indices"></param>
        public void Update(SharedGasTileOverlaySystem.GasOverlayData data, MapIndices indices)
        {
            DebugTools.Assert(InBounds(indices));
            var (offsetX, offsetY) = (indices.X - MapIndices.X,
                indices.Y - MapIndices.Y);
            
            TileData[offsetX, offsetY] = data;
        }

        public void Update(SharedGasTileOverlaySystem.GasOverlayData data, byte x, byte y)
        {
            DebugTools.Assert(x < SharedGasTileOverlaySystem.ChunkSize && y < SharedGasTileOverlaySystem.ChunkSize);

            TileData[x, y] = data;
        }

        public IEnumerable<SharedGasTileOverlaySystem.GasOverlayData> GetAllData()
        {
            for (var x = 0; x < SharedGasTileOverlaySystem.ChunkSize; x++)
            {
                for (var y = 0; y < SharedGasTileOverlaySystem.ChunkSize; y++)
                {
                    yield return TileData[x, y];
                }
            }
        }

        public void GetData(List<(MapIndices, SharedGasTileOverlaySystem.GasOverlayData)> existingData, HashSet<MapIndices> indices)
        {
            foreach (var index in indices)
            {
                existingData.Add((index, GetData(index)));
            }
        }

        public IEnumerable<MapIndices> GetAllIndices()
        {
            for (var x = 0; x < SharedGasTileOverlaySystem.ChunkSize; x++)
            {
                for (var y = 0; y < SharedGasTileOverlaySystem.ChunkSize; y++)
                {
                    yield return new MapIndices(MapIndices.X + x, MapIndices.Y + y);
                }
            }
        }

        public SharedGasTileOverlaySystem.GasOverlayData GetData(MapIndices indices)
        {
            DebugTools.Assert(InBounds(indices));
            return TileData[indices.X - MapIndices.X, indices.Y - MapIndices.Y];
        }

        private bool InBounds(MapIndices indices)
        {
            if (indices.X < MapIndices.X || indices.Y < MapIndices.Y) return false;
            if (indices.X >= MapIndices.X + SharedGasTileOverlaySystem.ChunkSize || indices.Y >= MapIndices.Y + SharedGasTileOverlaySystem.ChunkSize) return false;
            return true;
        }
    }
}