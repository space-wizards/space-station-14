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
        public readonly struct GasTileOverlayData
        {
            public readonly GridId GridIndex;
            public readonly MapIndices GridIndices;
            public readonly GasData[] GasData;

            public GasTileOverlayData(GridId gridIndex, MapIndices gridIndices, GasData[] gasData)
            {
                GridIndex = gridIndex;
                GridIndices = gridIndices;
                GasData = gasData;
            }

            public override int GetHashCode()
            {
                return GridIndex.GetHashCode() ^ GridIndices.GetHashCode() ^ GasData.GetHashCode();
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
