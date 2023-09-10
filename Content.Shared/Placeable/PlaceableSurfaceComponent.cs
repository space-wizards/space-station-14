using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Placeable
{
    [RegisterComponent, NetworkedComponent]
    [Access(typeof(PlaceableSurfaceSystem))]
    public sealed partial class PlaceableSurfaceComponent : Component
    {
        [DataField("isPlaceable")]
        public bool IsPlaceable { get; set; } = true;

        [DataField("placeCentered")]
        public bool PlaceCentered { get; set; }

        [DataField("positionOffset")]
        public Vector2 PositionOffset { get; set; }
    }

    [Serializable, NetSerializable]
    public sealed class PlaceableSurfaceComponentState : ComponentState
    {
        public readonly bool IsPlaceable;
        public readonly bool PlaceCentered;
        public readonly Vector2 PositionOffset;

        public PlaceableSurfaceComponentState(bool placeable, bool centered, Vector2 offset)
        {
            IsPlaceable = placeable;
            PlaceCentered = centered;
            PositionOffset = offset;
        }
    }
}
