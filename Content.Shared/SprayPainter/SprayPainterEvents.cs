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
public sealed class SprayPainterDecalPickedMessage(ProtoId<DecalPrototype> protoId) : BoundUserInterfaceMessage
{
    public ProtoId<DecalPrototype> DecalPrototype = protoId;
}

[Serializable, NetSerializable]
public sealed class SprayPainterDecalColorPickedMessage(Color? color) : BoundUserInterfaceMessage
{
    public Color? Color = color;
}

[Serializable, NetSerializable]
public sealed class SprayPainterDecalAnglePickedMessage(int angle) : BoundUserInterfaceMessage
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
public sealed partial class SprayPainterDoAfterEvent : DoAfterEvent
{
    [DataField]
    public string Prototype;

    [DataField]
    public string Category;

    [DataField]
    public PaintableVisuals Visuals;

    [DataField]
    public int Cost;

    public SprayPainterDoAfterEvent(string prototype, string category, PaintableVisuals visuals, int cost)
    {
        Prototype = prototype;
        Category = category;
        Visuals = visuals;
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
[Serializable, NetSerializable]
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
    public string Prototype = default!;

    /// <summary>
    /// The category of item being painted (e.g. lockers, airlocks, canisters).
    /// </summary>
    [DataField]
    public string Category = default!;
}
