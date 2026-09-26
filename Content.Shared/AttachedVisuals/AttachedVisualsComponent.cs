using Content.Shared.DisplacementMap;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared.AttachedVisuals;

/// <summary>
/// This is used for handling sprite visuals for items inside containers
/// </summary>
[RegisterComponent]
public sealed partial class AttachedVisualsComponent : Component
{
    /// <summary>
    /// Path to RSI to use for sprites
    /// </summary>
    [DataField("sprite")]
    public string? RsiPath;

    /// <summary>
    /// Attachment points that this entity supports
    /// </summary>
    [DataField]
    public List<AttachmentDefinition> Attachments = new();

    /// <summary>
    /// Visuals to show when this entity is in a given attachment slot
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<VisualAttachmentPrototype>, AttachedVisualLayers> AttachedVisuals = new();

    /// <summary>
    /// Entity origins, and the layers they are showing on this entity.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<EntityUid, List<AttachedLayer>> RevealedLayers = new();
}

/// <summary>
/// Defines sprite layers to show when this item is on a specific attachment point.
/// Also allows you to define sub-attachments, attachment points this item provides
/// only when it is attached to this point.
/// </summary>
[DataDefinition]
public sealed partial class AttachedVisualLayers
{
    /// <summary>
    /// Sprite layers to draw
    /// </summary>
    [DataField]
    public List<PrototypeLayerData>? Layers = new();

    /// <summary>
    /// List of extra sub-attachment slots that this slot can provide
    /// </summary>
    [DataField]
    public List<AttachmentDefinition>? Attachments = new();
}

/// <summary>
/// Defines a visual attachment slot. Things
/// Maps to a specific Container ID. You can have multiple attachments per container slot.
/// For example, the "shoes" container on a Urist can have attachments called "Boots" and "Shoes"
/// Those attachments can have different orders, so you can have boots over suits, but shoes under suits.
/// </summary>
[DataDefinition]
public sealed partial class AttachmentDefinition : IComparable<AttachmentDefinition>
{
    /// <summary>
    /// Container ID that this attachment maps to
    /// </summary>
    [DataField(required: true)]
    public string Container;

    /// <summary>
    /// Attachment ID
    /// </summary>
    [DataField(required: true)]
    public ProtoId<VisualAttachmentPrototype> Attachment;

    /// <summary>
    /// Default displacement data
    /// </summary>
    [DataField]
    public DisplacementData? DisplacementData;

    /// <summary>
    /// If we have sex displacement data, put it here
    /// </summary>
    [DataField]
    public Dictionary<Sex, DisplacementData>? SexedDisplacementData;

    /// <summary>
    /// Sorting order for sprite layering.
    /// 0 is the lowest/bottom layer (eg: jumpsuit).
    /// </summary>
    [DataField]
    public int Order = 0;

    public int CompareTo(AttachmentDefinition? other)
    {
        if (ReferenceEquals(this, other))
            return 0;

        if (other is null)
            return 1;

        var orderComparison = Order.CompareTo(other.Order);
        if (orderComparison != 0)
            return orderComparison;

        return Attachment.CompareTo(other.Attachment);
    }
}

public readonly record struct AttachedLayer
{
    /// <summary>
    /// The entity this layer is being provided by
    /// </summary>
    public readonly EntityUid Origin;

    /// <summary>
    /// The layer key
    /// </summary>
    public readonly string Key;

    /// <summary>
    /// Appearance MapKeys this layer has
    /// </summary>
    public readonly string[] MapKeys;

    /// <summary>
    /// The layer data itself
    /// </summary>
    public readonly PrototypeLayerData Data;

    public AttachedLayer(EntityUid origin, string key, string[] mapKeys, PrototypeLayerData data)
    {
        Origin = origin;
        Key = key;
        MapKeys = mapKeys;
        Data = data;
    }

    public void Deconstruct(out EntityUid origin, out string key, out string[] mapKeys, out PrototypeLayerData prototypeLayerData)
    {
        origin = Origin;
        key = Key;
        mapKeys = MapKeys;
        prototypeLayerData = Data;
    }
}
