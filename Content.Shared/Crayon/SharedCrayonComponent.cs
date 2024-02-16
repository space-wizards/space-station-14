using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Crayon
{
    [NetworkedComponent, ComponentProtoName("Crayon"), Access(typeof(SharedCrayonSystem))]
    public abstract partial class SharedCrayonComponent : Component
    {
        public string SelectedState { get; set; } = string.Empty;

        [DataField("color")] public Color Color;

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
    public sealed class CrayonColorMessage : BoundUserInterfaceMessage
    {
        public readonly Color Color;
        public CrayonColorMessage(Color color)
        {
            Color = color;
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
        public readonly Color Color;
        public readonly string State;
        public readonly int Charges;
        public readonly int Capacity;

        public CrayonComponentState(Color color, string state, int charges, int capacity)
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
        public bool SelectableColor;
        public Color Color;

        public CrayonBoundUserInterfaceState(string selected, bool selectableColor, Color color)
        {
            Selected = selected;
            SelectableColor = selectableColor;
            Color = color;
        }
    }
}
