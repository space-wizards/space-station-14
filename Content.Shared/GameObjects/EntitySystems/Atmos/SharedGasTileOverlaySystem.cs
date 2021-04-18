#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystems.Atmos
{
    public abstract class SharedGasTileOverlaySystem : EntitySystem
    {
        public const byte ChunkSize = 8;
        protected float AccumulatedFrameTime;

        public static Vector2i GetGasChunkIndices(Vector2i indices)
        {
            return new((int) Math.Floor((float) indices.X / ChunkSize) * ChunkSize, (int) MathF.Floor((float) indices.Y / ChunkSize) * ChunkSize);
        }

        [Serializable, NetSerializable]
        public readonly struct GasData : IEquatable<GasData>
        {
            public readonly byte Index;
            public readonly byte Opacity;

            public GasData(byte gasId, byte opacity)
            {
                Index = gasId;
                Opacity = opacity;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Index, Opacity);
            }

            public bool Equals(GasData other)
            {
                return other.Index == Index && other.Opacity == Opacity;
            }
        }

        [Serializable, NetSerializable]
        public readonly struct GasOverlayData : IEquatable<GasOverlayData>
        {
            public readonly byte FireState;
            public readonly float FireTemperature;
            public readonly GasData[] Gas;
            public readonly int HashCode;

            public GasOverlayData(byte fireState, float fireTemperature, GasData[] gas)
            {
                FireState = fireState;
                FireTemperature = fireTemperature;
                Gas = gas;

                Array.Sort(Gas, (a, b) => a.Index.CompareTo(b.Index));

                var hash = new HashCode();
                hash.Add(FireState);
                hash.Add(FireTemperature);

                foreach (var gasData in Gas)
                {
                    hash.Add(gasData);
                }

                HashCode = hash.ToHashCode();
            }

            public override int GetHashCode()
            {
                return HashCode;
            }

            public bool Equals(GasOverlayData other)
            {
                // If you revert this then you need to make sure the hash comparison between
                // our Gas[] and the other.Gas[] works.
                return HashCode == other.HashCode;
            }
        }

        /// <summary>
        ///     Invalid tiles for the gas overlay.
        ///     No point re-sending every tile if only a subset might have been updated.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class GasOverlayMessage : EntityEventArgs
        {
            public GridId GridId { get; }

            public List<(Vector2i, GasOverlayData)> OverlayData { get; }

            public GasOverlayMessage(GridId gridIndices, List<(Vector2i,GasOverlayData)> overlayData)
            {
                GridId = gridIndices;
                OverlayData = overlayData;
            }
        }
    }
}
