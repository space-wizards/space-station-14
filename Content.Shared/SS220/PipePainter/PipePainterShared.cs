// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.PipePainter;

// ReSharper disable RedundantLinebreak

[Serializable, NetSerializable]
public enum PipePainterUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class PipePainterSpritePickedMessage : BoundUserInterfaceMessage
{
    public string Key { get; }

    public PipePainterSpritePickedMessage(string key)
    {
        Key = key;
    }
}

[Serializable, NetSerializable]
public sealed class PipePainterBoundUserInterfaceState : BoundUserInterfaceState
{
    public string? SelectedColorKey { get; }
    public Dictionary<string, Color> Palette { get; }

    public PipePainterBoundUserInterfaceState(string? selectedColorKey, Dictionary<string, Color> palette)
    {
        SelectedColorKey = selectedColorKey;
        Palette = palette;
    }
}

[Serializable, NetSerializable]
public sealed class PipePainterDoAfterEvent : DoAfterEvent
{
    [DataField("color", required: true)]
    public readonly Color Color;

    private PipePainterDoAfterEvent()
    {
    }

    public PipePainterDoAfterEvent(Color color)
    {
        Color = color;
    }

    public override DoAfterEvent Clone() => this;
}
