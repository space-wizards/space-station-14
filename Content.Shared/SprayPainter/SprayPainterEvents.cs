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
    [DataField]
    public string Prototype;

    [DataField]
    public string Group;

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
    [DataField]
    public Color Color;

    public SprayPainterPipeDoAfterEvent(Color color)
    {
        Color = color;
    }

    public override DoAfterEvent Clone() => this;
}

/// <summary>
/// An action raised on an item when it was spray painted.
/// </summary>
[ByRefEvent]
public partial record struct EntityPaintedEvent(EntityUid? user, EntityUid tool, EntProtoId prototype, ProtoId<PaintableGroupPrototype> group)
{
    /// <summary>The entity painting this item.</summary>
    public EntityUid? User = user;
    /// <summary>The entity used to paint this item.</summary>
    public EntityUid Tool = tool;
    /// <summary>The prototype used to generate the new painted appearance.</summary>
    public EntProtoId Prototype = prototype;
    /// <summary>The group of item being painted (e.g. airlocks with glass, canisters).</summary>
    public ProtoId<PaintableGroupPrototype> Group = group;
}
