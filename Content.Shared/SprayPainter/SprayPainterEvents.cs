using Content.Shared.Decals;
using Content.Shared.DoAfter;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SprayPainter;

[Serializable, NetSerializable]
public enum SprayPainterUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SprayPainterSetDecalMessage(ProtoId<DecalPrototype> protoId) : BoundUserInterfaceMessage
{
    public ProtoId<DecalPrototype> DecalPrototype = protoId;
}

[Serializable, NetSerializable]
public sealed class SprayPainterSetDecalColorMessage(Color? color) : BoundUserInterfaceMessage
{
    public Color? Color = color;
}

[Serializable, NetSerializable]
public sealed class SprayPainterSetDecalSnapMessage(bool snap) : BoundUserInterfaceMessage
{
    public bool Snap = snap;
}

[Serializable, NetSerializable]
public sealed class SprayPainterSetDecalAngleMessage(int angle) : BoundUserInterfaceMessage
{
    public int Angle = angle;
}

[Serializable, NetSerializable]
public sealed class SprayPainterTabChangedMessage(int index, bool isSelectedTabWithDecals) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
    public readonly bool IsSelectedTabWithDecals = isSelectedTabWithDecals;
}

[Serializable, NetSerializable]
public sealed class SprayPainterSetPaintableStyleMessage(string group, string style) : BoundUserInterfaceMessage
{
    public readonly string Group = group;
    public readonly string Style = style;
}

[Serializable, NetSerializable]
public sealed class SprayPainterSetPipeColorMessage(string? key) : BoundUserInterfaceMessage
{
    public readonly string? Key = key;
}

[Serializable, NetSerializable]
public sealed partial class SprayPainterDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// The prototype to use to repaint this object.
    /// </summary>
    [DataField]
    public string Prototype;

    /// <summary>
    /// The group ID of the object being painted.
    /// </summary>
    [DataField]
    public string Group;

    /// <summary>
    /// The cost, in charges, to paint this object.
    /// </summary>
    [DataField]
    public int Cost;

    public SprayPainterDoAfterEvent(string prototype, string group, int cost)
    {
        Prototype = prototype;
        Group = group;
        Cost = cost;
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

/// <summary>
/// An action raised on an entity when it is spray painted.
/// </summary>
/// <param name="User">The entity painting this item.</param>
/// <param name="Tool">The entity used to paint this item.</param>
/// <param name="Prototype">The prototype used to generate the new painted appearance.</param>
/// <param name="Group">The group of the entity being painted (e.g. airlocks with glass, canisters).</param>
[ByRefEvent]
public partial record struct EntityPaintedEvent(
    EntityUid? User,
    EntityUid Tool,
    EntProtoId Prototype,
    ProtoId<PaintableGroupPrototype> Group);
