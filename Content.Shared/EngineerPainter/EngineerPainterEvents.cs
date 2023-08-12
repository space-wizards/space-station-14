using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.EngineerPainter
{
    [Serializable, NetSerializable]
    public enum EngineerPainterUiKey
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class EngineerPainterSpritePickedMessage : BoundUserInterfaceMessage
    {
        public int Index { get; }

        public EngineerPainterSpritePickedMessage(int index)
        {
            Index = index;
        }
    }

    [Serializable, NetSerializable]
    public sealed class EngineerPainterColorPickedMessage : BoundUserInterfaceMessage
    {
        public string? Key { get; }

        public EngineerPainterColorPickedMessage(string? key)
        {
            Key = key;
        }
    }

    [Serializable, NetSerializable]
    public sealed class EngineerPainterBoundUserInterfaceState : BoundUserInterfaceState
    {
        public int SelectedStyle { get; }
        public string? SelectedColorKey { get; }
        public Dictionary<string, Color> Palette { get; }

        public EngineerPainterBoundUserInterfaceState(int selectedStyle, string? selectedColorKey, Dictionary<string, Color> palette)
        {
            SelectedStyle = selectedStyle;
            SelectedColorKey = selectedColorKey;
            Palette = palette;
        }
    }

    [Serializable, NetSerializable]
    public sealed class EngineerPainterDoAfterEvent : DoAfterEvent
    {
        [DataField("sprite")]
        public readonly string? Sprite = null;

        [DataField("color")]
        public readonly Color? Color = null;

        private EngineerPainterDoAfterEvent()
        {
        }

        public EngineerPainterDoAfterEvent(string? sprite, Color? color)
        {
            Sprite = sprite;
            Color = color;
        }

        public override DoAfterEvent Clone() => this;
    }
}
