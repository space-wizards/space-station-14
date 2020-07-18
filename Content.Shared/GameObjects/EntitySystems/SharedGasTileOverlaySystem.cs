using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedTileOverlaySystem : EntitySystem
    {
        [Serializable, NetSerializable]
        public readonly struct TileOverlayData
        {
            public readonly GridId GridIndex;
            public readonly MapIndices GridIndices;
            public readonly int[] GasIndices;
            public readonly float[] Opacities;

            public TileOverlayData(GridId gridIndex, MapIndices gridIndices, int[] gasIndices, float[] opacities)
            {
                GridIndex = gridIndex;
                GridIndices = gridIndices;
                GasIndices = gasIndices;
                Opacities = opacities;
            }

            public override int GetHashCode()
            {
                return GridIndex.GetHashCode() ^ GridIndices.GetHashCode() ^ GasIndices.GetHashCode()
                       ^ Opacities.GetHashCode();
            }
        }

        [Serializable, NetSerializable]
        public class TileOverlayMessage : EntitySystemMessage
        {
            public TileOverlayData[] OverlayData { get; }
            public bool ClearAllOtherOverlays { get; }

            public TileOverlayMessage(TileOverlayData[] overlayData, bool clearAllOtherOverlays = false)
            {
                OverlayData = overlayData;
                ClearAllOtherOverlays = clearAllOtherOverlays;
            }
        }
    }
}
