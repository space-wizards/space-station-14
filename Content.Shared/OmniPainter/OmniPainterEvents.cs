using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.OmniPainter
{
    [Serializable, NetSerializable]
    public enum OmniPainterUiKey
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class OmniPainterSpritePickedMessage : BoundUserInterfaceMessage
    {
        public int Index { get; }

        public OmniPainterSpritePickedMessage(int index)
        {
            Index = index;
        }
    }

    [Serializable, NetSerializable]
    public sealed class OmniPainterColorPickedMessage : BoundUserInterfaceMessage
    {
        public string? Key { get; }

        public OmniPainterColorPickedMessage(string? key)
        {
            Key = key;
        }
    }

    [Serializable, NetSerializable]
    public sealed class OmniPainterBoundUserInterfaceState : BoundUserInterfaceState
    {
        public int SelectedStyle { get; }
        public string? SelectedColorKey { get; }
        public Dictionary<string, Color> Palette { get; }

        public OmniPainterBoundUserInterfaceState(int selectedStyle, string? selectedColorKey, Dictionary<string, Color> palette)
        {
            SelectedStyle = selectedStyle;
            SelectedColorKey = selectedColorKey;
            Palette = palette;
        }
    }

    [Serializable, NetSerializable]
    public sealed class OmniPainterDoAfterEvent : DoAfterEvent
    {
        [DataField("sprite")]
        public readonly string? Sprite = null;

        [DataField("color")]
        public readonly Color? Color = null;

        private OmniPainterDoAfterEvent()
        {
        }

        public OmniPainterDoAfterEvent(string? sprite, Color? color)
        {
            Sprite = sprite;
            Color = color;
        }

        public override DoAfterEvent Clone() => this;
    }
}
