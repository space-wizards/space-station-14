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
    public readonly int Index;

    public SprayPainterSpritePickedMessage(int index)
    {
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
public sealed partial class SprayPainterDoorDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// Base RSI path to set for the door sprite.
    /// </summary>
    [DataField]
    public string Sprite;

    /// <summary>
    /// Department id to set for the door, if the style has one.
    /// </summary>
    [DataField]
    public string? Department;

    public SprayPainterDoorDoAfterEvent(string sprite, string? department)
    {
        Sprite = sprite;
        Department = department;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class SprayPainterPipeDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// Color of the pipe to set.
    /// </summary>
    [DataField]
    public Color Color;

    public SprayPainterPipeDoAfterEvent(Color color)
    {
        Color = color;
    }

    public override DoAfterEvent Clone() => this;
}
