using System.Collections.Generic;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Atmos
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
        public Vector2i Vector2i { get; }

        public SharedGasTileOverlaySystem.GasOverlayData[,] TileData = new SharedGasTileOverlaySystem.GasOverlayData[SharedGasTileOverlaySystem.ChunkSize, SharedGasTileOverlaySystem.ChunkSize];

        public GameTick LastUpdate { get; private set; }

        public GasOverlayChunk(GridId gridIndices, Vector2i vector2i)
        {
            GridIndices = gridIndices;
            Vector2i = vector2i;
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
        public void Update(SharedGasTileOverlaySystem.GasOverlayData data, Vector2i indices)
        {
            DebugTools.Assert(InBounds(indices));
            var (offsetX, offsetY) = (indices.X - Vector2i.X,
                indices.Y - Vector2i.Y);

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

        public void GetData(List<(Vector2i, SharedGasTileOverlaySystem.GasOverlayData)> existingData, HashSet<Vector2i> indices)
        {
            foreach (var index in indices)
            {
                existingData.Add((index, GetData(index)));
            }
        }

        public IEnumerable<Vector2i> GetAllIndices()
        {
            for (var x = 0; x < SharedGasTileOverlaySystem.ChunkSize; x++)
            {
                for (var y = 0; y < SharedGasTileOverlaySystem.ChunkSize; y++)
                {
                    yield return new Vector2i(Vector2i.X + x, Vector2i.Y + y);
                }
            }
        }

        public SharedGasTileOverlaySystem.GasOverlayData GetData(Vector2i indices)
        {
            DebugTools.Assert(InBounds(indices));
            return TileData[indices.X - Vector2i.X, indices.Y - Vector2i.Y];
        }

        private bool InBounds(Vector2i indices)
        {
            if (indices.X < Vector2i.X || indices.Y < Vector2i.Y) return false;
            if (indices.X >= Vector2i.X + SharedGasTileOverlaySystem.ChunkSize || indices.Y >= Vector2i.Y + SharedGasTileOverlaySystem.ChunkSize) return false;
            return true;
        }
    }
}
