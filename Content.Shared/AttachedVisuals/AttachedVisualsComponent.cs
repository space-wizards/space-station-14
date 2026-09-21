using System.Diagnostics.CodeAnalysis;
using Content.Shared.DisplacementMap;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared.AttachedVisuals;

/// <summary>
/// This is used for...
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
    public Dictionary<ProtoId<VisualAttachmentPrototype>, AttachedVisualDefinition> AttachedVisuals = new();

    [ViewVariables]
    public readonly Dictionary<EntityUid, List<string>> RevealedLayers = new();
}

[DataDefinition]
public sealed partial class AttachedVisualDefinition
{
    /// <summary>
    /// Sprite layers to draw
    /// </summary>
    [DataField]
    public List<PrototypeLayerData> Layers = new();

    /// <summary>
    /// List of extra attachments slots that this slot can provide
    /// </summary>
    [DataField]
    public List<AttachmentDefinition> Attachments = new();
}

[DataDefinition]
public sealed partial class AttachmentDefinition
{
    /// <summary>
    /// Container ID that this attachment maps to
    /// </summary>
    [DataField]
    public string Container;

    /// <summary>
    /// Attachment ID
    /// </summary>
    [DataField]
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
}

[Prototype]
public sealed partial class VisualAttachmentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;
}

[ByRefEvent]
public record struct GetAttachedVisualsEvent
{
    private readonly string _childPrefix;

    public readonly VisualAttachmentPrototype AttachmentName;

    public readonly List<(EntityUid, string, HashSet<string>, PrototypeLayerData)> Layers;

    public GetAttachedVisualsEvent(VisualAttachmentPrototype attachmentName, string childPrefix, List<(EntityUid, string, HashSet<string>, PrototypeLayerData)> layers)
    {
        _childPrefix = childPrefix;
        Layers = layers;
        AttachmentName = attachmentName;
    }

    public void AddLayer(EntityUid owner, HashSet<string> mapKeys, PrototypeLayerData layer)
    {
        Layers.Add((owner, $"{_childPrefix}-{Layers.Count}", mapKeys, layer));
    }
}


[ByRefEvent]
public record struct AttachedVisualsUpdatedEvent
{
    /// <summary>
    /// Entity our sprite layers were drawn onto
    /// </summary>
    public EntityUid AttachedTo;

    /// <summary>
    /// List of layer indexes
    /// </summary>
    public Dictionary<object, int> LayerMap;

    public AttachedVisualsUpdatedEvent(EntityUid attachedTo, Dictionary<object, int> layerMap)
    {
        AttachedTo = attachedTo;
        LayerMap = layerMap;
    }

    public bool TryGetLayerIndex(Enum key, [NotNullWhen(true)] out int? index)
    {
        index = null;

        if (!LayerMap.TryGetValue(key, out var layerIndex))
            return false;

        index = layerIndex;
        return true;
    }
}
