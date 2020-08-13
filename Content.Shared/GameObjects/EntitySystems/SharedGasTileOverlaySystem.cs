using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedGasTileOverlaySystem : EntitySystem
    {
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
        public readonly struct GasOverlayData
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
        }

        [Serializable, NetSerializable]
        public readonly struct GasTileOverlayData
        {
            public readonly GridId GridIndex;
            public readonly MapIndices GridIndices;
            public readonly GasOverlayData Data;

            public GasTileOverlayData(GridId gridIndex, MapIndices gridIndices, GasOverlayData data)
            {
                GridIndex = gridIndex;
                GridIndices = gridIndices;
                Data = data;
            }

            public override int GetHashCode()
            {
                return GridIndex.GetHashCode() ^ GridIndices.GetHashCode() ^ Data.GetHashCode();
            }
        }

        [Serializable, NetSerializable]
        public class GasTileOverlayMessage : EntitySystemMessage
        {
            public GasTileOverlayData[] OverlayData { get; }
            public bool ClearAllOtherOverlays { get; }

            public GasTileOverlayMessage(GasTileOverlayData[] overlayData, bool clearAllOtherOverlays = false)
            {
                OverlayData = overlayData;
                ClearAllOtherOverlays = clearAllOtherOverlays;
            }
        }
    }
}
