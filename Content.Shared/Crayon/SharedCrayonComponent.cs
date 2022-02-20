using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Crayon
{
    [NetworkedComponent, ComponentProtoName("Crayon")]
    public abstract class SharedCrayonComponent : Component
    {
        public string SelectedState { get; set; } = string.Empty;

        [DataField("color")] public string _color = "white";

        [Serializable, NetSerializable]
        public enum CrayonUiKey : byte
        {
            Key,
        }
    }

    [Serializable, NetSerializable]
    public sealed class CrayonSelectMessage : BoundUserInterfaceMessage
    {
        public readonly string State;
        public CrayonSelectMessage(string selected)
        {
            State = selected;
        }
    }

    [Serializable, NetSerializable]
    public enum CrayonVisuals
    {
        State,
        Color
    }

    [Serializable, NetSerializable]
    public sealed class CrayonComponentState : ComponentState
    {
        public readonly string Color;
        public readonly string State;
        public readonly int Charges;
        public readonly int Capacity;

        public CrayonComponentState(string color, string state, int charges, int capacity)
        {
            Color = color;
            State = state;
            Charges = charges;
            Capacity = capacity;
        }
    }
    [Serializable, NetSerializable]
    public sealed class CrayonBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string Selected;
        public Color Color;

        public CrayonBoundUserInterfaceState(string selected, Color color)
        {
            Selected = selected;
            Color = color;
        }
    }
}
