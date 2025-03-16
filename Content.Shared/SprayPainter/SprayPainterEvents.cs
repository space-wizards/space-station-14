using Content.Shared.DoAfter;
using Content.Shared.SprayPainter.Components;
using Content.Shared.SprayPainter.Prototypes;
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
    public readonly string Category;
    public readonly int Index;

    public SprayPainterSpritePickedMessage(string category, int index)
    {
        Category = category;
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed class SprayPainterColorPickedMessage : BoundUserInterfaceMessage
{
    public readonly string? Key;

    public SprayPainterColorPickedMessage(string? key)
    {
        Key = key;
    }
}

[Serializable, NetSerializable]
public sealed class SprayPainterBoundUserInterfaceState : BoundUserInterfaceState
{
    public Dictionary<string, int> SelectedStyles { get; }
    public string? SelectedColorKey { get; }
    public Dictionary<string, Color> Palette { get; }

    public SprayPainterBoundUserInterfaceState(Dictionary<string, int> selectedStyles, string? selectedColorKey, Dictionary<string, Color> palette)
    {
        SelectedStyles = selectedStyles;
        SelectedColorKey = selectedColorKey;
        Palette = palette;
    }
}

[Serializable, NetSerializable]
public sealed partial class SprayPainterDoAfterEvent : DoAfterEvent
{
    [DataField]
    public string Prototype;

    [DataField]
    public string Category;

    [DataField]
    public PaintableVisuals Visuals;

    public SprayPainterDoAfterEvent(string prototype, string category, PaintableVisuals visuals)
    {
        Prototype = prototype;
        Category = category;
        Visuals = visuals;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class SprayPainterPipeDoAfterEvent : DoAfterEvent
{
    [DataField]
    public Color Color;

    public SprayPainterPipeDoAfterEvent(Color color)
    {
        Color = color;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class SprayPainterCanisterDoAfterEvent : DoAfterEvent
{

    [DataField]
    public string Prototype;

    [DataField]
    public string Category;

    public override DoAfterEvent Clone() => this;
}
