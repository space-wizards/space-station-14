using System.Diagnostics.CodeAnalysis;
using Content.Shared.DisplacementMap;
using Robust.Shared.Prototypes;

namespace Content.Shared.AttachedVisuals;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class AttachedVisualsComponent : Component
{
    [DataField("sprite")]
    public string? RsiPath;

    //Mapping to Container ID (on this entity) -> View name
    [DataField]
    public List<AttachmentDefinition> Attachments = new();

    [DataField]
    public Dictionary<ProtoId<VisualAttachmentPrototype>, AttachedVisualDefinition> AttachedVisuals = new();

    [ViewVariables]
    public readonly Dictionary<EntityUid, List<string>> RevealedLayers = new();
}

[DataDefinition]
public sealed partial class AttachedVisualDefinition
{
    [DataField]
    public List<PrototypeLayerData> Layers = new();

    // Container ID -> view name,
    [DataField]
    public List<AttachmentDefinition> Attachments = new();
}

[DataDefinition]
public sealed partial class AttachmentDefinition
{
    [DataField]
    public string Container;

    [DataField]
    public ProtoId<VisualAttachmentPrototype> Attachment;


    [DataField]
    public DisplacementData? DisplacementData;
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
