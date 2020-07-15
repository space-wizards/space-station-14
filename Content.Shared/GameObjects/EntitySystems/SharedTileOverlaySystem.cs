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
            public readonly string[] Overlays;
            public readonly string[] AnimatedOverlays;
            public readonly string[] AnimatedOverlayStates;

            public TileOverlayData(GridId gridIndex, MapIndices gridIndices, string[] overlays, string[] animatedOverlays, string[] animatedOverlayStates)
            {
                GridIndex = gridIndex;
                GridIndices = gridIndices;
                Overlays = overlays;
                AnimatedOverlays = animatedOverlays;
                AnimatedOverlayStates = animatedOverlayStates;
            }

            public override int GetHashCode()
            {
                return GridIndex.GetHashCode() ^ GridIndices.GetHashCode() ^ Overlays.GetHashCode()
                       ^ AnimatedOverlays.GetHashCode() ^ AnimatedOverlayStates.GetHashCode();
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
