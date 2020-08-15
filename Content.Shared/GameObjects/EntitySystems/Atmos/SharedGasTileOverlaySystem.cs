using System;
using System.Collections.Generic;
using System.Linq;
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
        
        /// <summary>
        ///     How frequently we can send out new data
        /// </summary>
        protected const float UpdateTime = 0.16f;
        protected float AccumulatedFrameTime;

        // Probably need a cvar for this.
        public const float UpdateRange = 18;
        
        public static MapIndices GetGasChunkIndices(MapIndices indices)
        {
            return new MapIndices((int) Math.Floor((float) indices.X / ChunkSize) * ChunkSize, (int) MathF.Floor((float) indices.Y / ChunkSize) * ChunkSize);
        }
        
        [Serializable, NetSerializable]
        public struct GasData
        {
            public int Index { get; set; }
            public float Opacity { get; set; }

            public GasData(int gasId, float opacity)
            {
                Index = gasId;
                Opacity = opacity;
            }
        }

        [Serializable, NetSerializable]
        public readonly struct GasOverlayData : IEquatable<GasOverlayData>
        {
            public readonly int FireState;
            public readonly float FireTemperature;
            public readonly GasData[] Gas;

            public GasOverlayData(int fireState, float fireTemperature, GasData[] gas)
            {
                FireState = fireState;
                FireTemperature = fireTemperature;
                Gas = gas;
            }

            public bool Equals(GasOverlayData other)
            {
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
        public sealed class GasOverlayMessage : ComponentMessage
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

    public abstract class SharedCanSeeGasesComponent : Component
    {
        public override string Name => "CanSeeGases";
        public override uint? NetID => ContentNetIDs.GAS_OVERLAY;
    }
}
