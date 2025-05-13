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
public sealed class SprayPainterSetPaintablePrototypeMessage : BoundUserInterfaceMessage
{
    public readonly string Category;
    public readonly int Index;

    public SprayPainterSetPaintablePrototypeMessage(string category, int index)
    {
        Category = category;
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed class SprayPainterSetPipeColorMessage : BoundUserInterfaceMessage
{
    public readonly string? Key;

    public SprayPainterSetPipeColorMessage(string? key)
    {
        Key = key;
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
    public int Cost;

    public SprayPainterDoAfterEvent(string prototype, string category, int cost)
    {
        Prototype = prototype;
        Category = category;
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
/// An action raised on an item when it's spray painted.
/// </summary>
public sealed partial class EntityPaintedEvent : EntityEventArgs
{
    /// <summary>
    /// The entity painting this item.
    /// </summary>
    [DataField]
    public EntityUid? User = default!;

    /// <summary>
    /// The entity used to paint this item.
    /// </summary>
    [DataField]
    public EntityUid Tool = default!;

    /// <summary>
    /// The prototype being used to generate the new painted appearance.
    /// </summary>
    [DataField]
    public EntProtoId Prototype = default!;

    /// <summary>
    /// The category of item being painted (e.g. lockers, airlocks, canisters).
    /// </summary>
    [DataField]
    public ProtoId<PaintableGroupCategoryPrototype> Category = default!;
}
