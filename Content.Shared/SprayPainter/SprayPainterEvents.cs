using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SprayPainter;

[Serializable, NetSerializable]
public enum SprayPainterUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SprayPainterSpritePickedMessage : BoundUserInterfaceMessage
{
    public int Index { get; }

    public SprayPainterSpritePickedMessage(int index)
    {
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed class SprayPainterColorPickedMessage : BoundUserInterfaceMessage
{
    public string? Key { get; }

    public SprayPainterColorPickedMessage(string? key)
    {
        Key = key;
    }
}

[Serializable, NetSerializable]
public sealed class SprayPainterBoundUserInterfaceState : BoundUserInterfaceState
{
    public int SelectedStyle { get; }
    public string? SelectedColorKey { get; }
    public Dictionary<string, Color> Palette { get; }

    public SprayPainterBoundUserInterfaceState(int selectedStyle, string? selectedColorKey, Dictionary<string, Color> palette)
    {
        SelectedStyle = selectedStyle;
        SelectedColorKey = selectedColorKey;
        Palette = palette;
    }
}

[Serializable, NetSerializable]
public sealed partial class SprayPainterDoAfterEvent : DoAfterEvent
{
    [DataField("sprite")]
    public string? Sprite = null;

    [DataField("color")]
    public Color? Color = null;

    private SprayPainterDoAfterEvent()
    {
    }

    public SprayPainterDoAfterEvent(string? sprite, Color? color)
    {
        Sprite = sprite;
        Color = color;
    }

    public override DoAfterEvent Clone() => this;
}
