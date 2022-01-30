using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Placeable
{
    [RegisterComponent, NetworkedComponent, ComponentProtoName("PlaceableSurface")]
    public sealed class PlaceableSurfaceComponent : Component
    {
        [ViewVariables]
        [DataField("isPlaceable")]
        public bool IsPlaceable { get; set; } = true;

        [ViewVariables]
        [DataField("placeCentered")]
        public bool PlaceCentered { get; set; }

        [ViewVariables]
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
