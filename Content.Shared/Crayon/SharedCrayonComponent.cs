#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components
{
    public class SharedCrayonComponent : Component
    {
        public override string Name => "Crayon";
        public override uint? NetID => ContentNetIDs.CRAYONS;

        public string SelectedState { get; set; } = string.Empty;

        [DataField("color")]
        protected string _color = "white";

        [Serializable, NetSerializable]
        public enum CrayonUiKey
        {
            Key,
        }
    }

    [Serializable, NetSerializable]
    public class CrayonSelectMessage : BoundUserInterfaceMessage
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
        Color,
        Rotation
    }

    [Serializable, NetSerializable]
    public class CrayonComponentState : ComponentState
    {
        public readonly string Color;
        public readonly string State;
        public readonly int Charges;
        public readonly int Capacity;

        public CrayonComponentState(string color, string state, int charges, int capacity) : base(ContentNetIDs.CRAYONS)
        {
            Color = color;
            State = state;
            Charges = charges;
            Capacity = capacity;
        }
    }
    [Serializable, NetSerializable]
    public class CrayonBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string Selected;
        public Color Color;

        public CrayonBoundUserInterfaceState(string selected, Color color)
        {
            Selected = selected;
            Color = color;
        }
    }

    [Serializable, NetSerializable, Prototype("crayonDecal")]
    public class CrayonDecalPrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("spritePath")] public string SpritePath { get; } = string.Empty;

        [DataField("decals")] public List<string> Decals { get; } = new();
    }
}
