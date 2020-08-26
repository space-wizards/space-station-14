using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.GameObjects.EntitySystems.Atmos
{
    public abstract class SharedGasTileOverlaySystem : EntitySystem
    {
        public const byte ChunkSize = 8;
        protected float AccumulatedFrameTime;

        public static MapIndices GetGasChunkIndices(MapIndices indices)
        {
            return new MapIndices((int) Math.Floor((float) indices.X / ChunkSize) * ChunkSize, (int) MathF.Floor((float) indices.Y / ChunkSize) * ChunkSize);
        }

        [Serializable, NetSerializable]
        public struct GasData
        {
            public byte Index { get; set; }
            public byte Opacity { get; set; }

            public GasData(byte gasId, byte opacity)
            {
                Index = gasId;
                Opacity = opacity;
            }
        }

        [Serializable, NetSerializable]
        public readonly struct GasOverlayData : IEquatable<GasOverlayData>
        {
            public readonly byte FireState;
            public readonly float FireTemperature;
            public readonly GasData[] Gas;

            public GasOverlayData(byte fireState, float fireTemperature, GasData[] gas)
            {
                FireState = fireState;
                FireTemperature = fireTemperature;
                Gas = gas;
            }

            public bool Equals(GasOverlayData other)
            {
                // TODO: Moony had a suggestion on how to do this faster with the hash
                // https://discordapp.com/channels/310555209753690112/310555209753690112/744080145219846204
                // Aside from that I can't really see any low-hanging fruit CPU perf wise.
                if (Gas?.Length != other.Gas?.Length) return false;
                if (FireState != other.FireState) return false;
                if (FireTemperature != other.FireTemperature) return false;

                if (Gas == null)
                {
                    return true;
                }

                DebugTools.Assert(other.Gas != null);

                for (var i = 0; i < Gas.Length; i++)
                {
                    var thisGas = Gas[i];
                    var otherGas = other.Gas[i];

                    if (!thisGas.Equals(otherGas))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        ///     Invalid tiles for the gas overlay.
        ///     No point re-sending every tile if only a subset might have been updated.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class GasOverlayMessage : EntitySystemMessage
        {
            public GridId GridId { get; }

            public List<(MapIndices, GasOverlayData)> OverlayData { get; }

            public GasOverlayMessage(GridId gridIndices, List<(MapIndices,GasOverlayData)> overlayData)
            {
                GridId = gridIndices;
                OverlayData = overlayData;
            }
        }
    }
}
