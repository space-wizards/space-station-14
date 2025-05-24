using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SprayPainter.AtmosPipes;

[Serializable, NetSerializable]
public sealed class AtmosPipePainterColorPickedMessage(string? key) : BoundUserInterfaceMessage
{
    public readonly string? Key = key;
}

[Serializable, NetSerializable]
public sealed partial class AtmosPipePainterDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// Color of the pipe to set.
    /// </summary>
    [DataField]
    public Color Color;

    public AtmosPipePainterDoAfterEvent(Color color)
    {
        Color = color;
    }

    public override DoAfterEvent Clone() => new AtmosPipePainterDoAfterEvent(Color);
}
