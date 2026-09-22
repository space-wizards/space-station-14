using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.AttachedVisuals;

[ByRefEvent]
public record struct AttachedVisualsUpdatedEvent
{
    /// <summary>
    /// Entity our sprite layers were drawn onto
    /// </summary>
    public EntityUid AttachedTo;

    /// <summary>
    /// List of appearance MapKeys and the layer key they map to
    /// </summary>
    public Dictionary<object, string> LayerMap;

    public AttachedVisualsUpdatedEvent(EntityUid attachedTo, Dictionary<object, string> layerMap)
    {
        AttachedTo = attachedTo;
        LayerMap = layerMap;
    }

    public bool TryGetLayerKey(Enum key, [NotNullWhen(true)] out string? layerKey)
    {
        layerKey = null;

        if (!LayerMap.TryGetValue(key, out var layerIndex))
            return false;

        layerKey = layerIndex;
        return true;
    }
}
