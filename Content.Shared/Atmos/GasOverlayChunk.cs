using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared.Atmos.EntitySystems.SharedGasTileOverlaySystem;

namespace Content.Shared.Atmos
{
    [Serializable, NetSerializable]
    [Access(typeof(SharedGasTileOverlaySystem))]
    public sealed class GasOverlayChunk
    {
        /// <summary>
        ///     The index of this chunk
        /// </summary>
        public readonly Vector2i Index;
        public readonly Vector2i Origin;

        public GasOverlayData[][] TileData = new GasOverlayData[ChunkSize][];

        [NonSerialized]
        public GameTick LastUpdate;

        public GasOverlayChunk(Vector2i index)
        {
            Index = index;
            Origin = Index * ChunkSize;

            // For whatever reason, net serialize does not like multi_D arrays. So Jagged it is.
            for (var i = 0; i < ChunkSize; i++)
            {
                TileData[i] = new GasOverlayData[ChunkSize];
            }
        }

        public GasOverlayChunk(GasOverlayChunk data)
        {
            Index = data.Index;
            Origin = data.Origin;
            for (int i = 0; i < ChunkSize; i++)
            {
                // This does not clone the opacity array. However, this chunk cloning is only used by the client,
                // which never modifies that directly. So this should be fine.
                var array = TileData[i] = new GasOverlayData[ChunkSize];
                Array.Copy(data.TileData[i], array, ChunkSize);
            }
        }

        public ref GasOverlayData GetData(Vector2i gridIndices)
        {
            DebugTools.Assert(InBounds(gridIndices));
            return ref TileData[gridIndices.X - Origin.X][gridIndices.Y - Origin.Y];
        }

        private bool InBounds(Vector2i gridIndices)
        {
            return gridIndices.X >= Origin.X &&
                gridIndices.Y >= Origin.Y &&
                gridIndices.X < Origin.X + ChunkSize &&
                gridIndices.Y < Origin.Y + ChunkSize;
        }
    }

    public struct GasChunkEnumerator
    {
        private GasOverlayChunk _chunk;
        public int X = 0;
        public int Y = -1;
        private GasOverlayData[] _column;


        public GasChunkEnumerator(GasOverlayChunk chunk)
        {
            _chunk = chunk;
            _column = _chunk.TileData[0];
        }

        public bool MoveNext(out GasOverlayData gas)
        {
            while (X < ChunkSize)
            {
                // We want to increment Y before returning, but we also want it to match the current Y coordinate for
                // the returned gas, so using a slightly different logic for the Y loop.
                while (Y < ChunkSize - 1)
                {
                    Y++;
                    gas = _column[Y];

                    if (!gas.Equals(default))
                        return true;
                }

                X++;
                if (X < ChunkSize)
                    _column = _chunk.TileData[X];
                Y = -1;
            }

            gas = default;
            return false;
        }
    }
}
