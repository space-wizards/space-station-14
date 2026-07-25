using System.Numerics;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared.Atmos.EntitySystems.SharedGasTileOverlaySystem;

namespace Content.Shared.Atmos
{
    [RegisterComponent, NetworkedComponent]
    [Access(typeof(SharedGasTileOverlaySystem))]
    public sealed partial class GasOverlayChunkComponent : Component
    {
        public GasOverlayFireData[] FireData = new GasOverlayFireData[ChunkSize * ChunkSize];
        public GasOverlayOpacityData[] OpacityData = new GasOverlayOpacityData[ChunkSize * ChunkSize];
        public GasOverlayTemperatureData[] TemperatureData = new GasOverlayTemperatureData[ChunkSize * ChunkSize];

        [NonSerialized]
        public GameTick[] FireLastModified = new GameTick[ChunkSize * ChunkSize];

        [NonSerialized]
        public GameTick[] OpacityLastModified = new GameTick[ChunkSize * ChunkSize];

        [NonSerialized]
        public GameTick[] TemperatureLastModified = new GameTick[ChunkSize * ChunkSize];

        /// <summary>
        /// Resolve a data index into the chunk data arrays for the given grid index.
        /// </summary>
        public static int GetDataIndex(Vector2i chunkIndex, Vector2i gridIndices)
        {
            DebugTools.Assert(InBounds(chunkIndex, gridIndices));
            var origin = chunkIndex * ChunkSize;
            return (gridIndices.X - origin.X) + (gridIndices.Y - origin.Y) * ChunkSize;
        }

        private static bool InBounds(Vector2i chunkIndex, Vector2i gridIndices)
        {
            var origin = chunkIndex * ChunkSize;
            return gridIndices.X >= origin.X &&
                gridIndices.Y >= origin.Y &&
                gridIndices.X < origin.X + ChunkSize &&
                gridIndices.Y < origin.Y + ChunkSize;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GasOverlayChunkState(
        GasOverlayFireData[] fireData,
        GasOverlayOpacityData[] opacityData,
        GasOverlayTemperatureData[] temperatureData) : ComponentState
    {
        public readonly GasOverlayFireData[] FireData = fireData;
        public readonly GasOverlayOpacityData[] OpacityData = opacityData;
        public readonly GasOverlayTemperatureData[] TemperatureData = temperatureData;
    }

    [Serializable, NetSerializable]
    public sealed class GasOverlayChunkDeltaState(
        GasOverlayFireDelta[] modifiedFire,
        GasOverlayOpacityDelta[] modifiedOpacity,
        GasOverlayTemperatureDelta[] modifiedTemperature)
        : ComponentState, IComponentDeltaState<GasOverlayChunkState>
    {
        public readonly GasOverlayFireDelta[] ModifiedFire = modifiedFire;
        public readonly GasOverlayOpacityDelta[] ModifiedOpacity = modifiedOpacity;
        public readonly GasOverlayTemperatureDelta[] ModifiedTemperature = modifiedTemperature;

        public void ApplyToFullState(GasOverlayChunkState state)
        {
            foreach (var delta in ModifiedFire)
            {
                state.FireData[delta.Index] = delta.Data;
            }

            foreach (var delta in ModifiedOpacity)
            {
                state.OpacityData[delta.Index] = delta.Data;
            }

            foreach (var delta in ModifiedTemperature)
            {
                state.TemperatureData[delta.Index] = delta.Data;
            }
        }

        public GasOverlayChunkState CreateNewFullState(GasOverlayChunkState state)
        {
            var fireData = new GasOverlayFireData[state.FireData.Length];
            var opacityData = new GasOverlayOpacityData[state.OpacityData.Length];
            var temperatureData = new GasOverlayTemperatureData[state.TemperatureData.Length];

            Array.Copy(state.FireData, fireData, state.FireData.Length);
            Array.Copy(state.OpacityData, opacityData, state.OpacityData.Length);
            Array.Copy(state.TemperatureData, temperatureData, state.TemperatureData.Length);

            var newState = new GasOverlayChunkState(fireData, opacityData, temperatureData);
            ApplyToFullState(newState);
            return newState;
        }
    }

    [Serializable, NetSerializable]
    public readonly struct GasOverlayFireDelta(byte index, GasOverlayFireData data)
    {
        public readonly byte Index = index;
        public readonly GasOverlayFireData Data = data;
    }

    [Serializable, NetSerializable]
    public readonly struct GasOverlayOpacityDelta(byte index, GasOverlayOpacityData data)
    {
        public readonly byte Index = index;
        public readonly GasOverlayOpacityData Data = data;
    }

    [Serializable, NetSerializable]
    public readonly struct GasOverlayTemperatureDelta(byte index, GasOverlayTemperatureData data)
    {
        public readonly byte Index = index;
        public readonly GasOverlayTemperatureData Data = data;
    }

    public struct GasOverlayInvalidTileMask
    {
        public ulong Mask0;
        public ulong Mask1;
        public ulong Mask2;
        public ulong Mask3;

        public readonly bool IsEmpty => (Mask0 | Mask1 | Mask2 | Mask3) == 0;

        public void Add(int tileIndex)
        {
            DebugTools.Assert(tileIndex >= 0 && tileIndex < ChunkSize * ChunkSize);
            var bit = 1UL << (tileIndex & 63);
            switch (tileIndex / 64)
            {
                case 0:
                    Mask0 |= bit;
                    break;
                case 1:
                    Mask1 |= bit;
                    break;
                case 2:
                    Mask2 |= bit;
                    break;
                default:
                    Mask3 |= bit;
                    break;
            }
        }

        public bool TryPop(out int tileIndex)
        {
            if (TryPop(ref Mask0, 0, out tileIndex) ||
                TryPop(ref Mask1, 64, out tileIndex) ||
                TryPop(ref Mask2, 128, out tileIndex) ||
                TryPop(ref Mask3, 192, out tileIndex))
            {
                return true;
            }

            tileIndex = default;
            return false;
        }

        private static bool TryPop(ref ulong mask, int offset, out int tileIndex)
        {
            if (mask == 0)
            {
                tileIndex = default;
                return false;
            }

            var bit = BitOperations.TrailingZeroCount(mask);
            mask &= ~(1UL << bit);
            tileIndex = offset + bit;
            return true;
        }
    }

    public struct GasChunkEnumerator
    {
        private readonly GasOverlayFireData[] _fireData;
        private readonly GasOverlayOpacityData[] _opacityData;
        private readonly GasOverlayTemperatureData[] _temperatureData;
        private int _index = -1;

        public int X = ChunkSize - 1;
        public int Y = -1;

        public GasChunkEnumerator(GasOverlayChunkComponent chunk)
        {
            _fireData = chunk.FireData;
            _opacityData = chunk.OpacityData;
            _temperatureData = chunk.TemperatureData;
        }

        public bool MoveNext(out GasOverlayData gas)
        {
            while (++_index < _fireData.Length)
            {
                X += 1;
                if (X >= ChunkSize)
                {
                    X = 0;
                    Y += 1;
                }

                var fire = _fireData[_index];
                var opacity = _opacityData[_index];
                var temperature = _temperatureData[_index];

                if (fire.Equals(default) &&
                    opacity.Equals(default) &&
                    temperature.Equals(default))
                {
                    continue;
                }

                gas = new GasOverlayData(fire.FireState, opacity.PackedOpacity, temperature.ByteGasTemperature);
                if (temperature.Active || !gas.Equals(default))
                    return true;
            }

            gas = default;
            return false;
        }
    }
}
