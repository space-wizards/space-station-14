using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SprayPainter.Airlocks;

[Serializable, NetSerializable]
public sealed class AirlockPainterSpritePickedMessage(int index) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
}

[Serializable, NetSerializable]
public sealed partial class AirlockPainterDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// Base RSI path to set for the door sprite.
    /// </summary>
    [DataField]
    public ResPath Sprite;

    /// <summary>
    /// Department id to set for the door, if the style has one.
    /// </summary>
    [DataField]
    public string? Department;

    public AirlockPainterDoAfterEvent(ResPath sprite, string? department)
    {
        Sprite = sprite;
        Department = department;
    }

    public override DoAfterEvent Clone() => new AirlockPainterDoAfterEvent(Sprite, Department);
}
