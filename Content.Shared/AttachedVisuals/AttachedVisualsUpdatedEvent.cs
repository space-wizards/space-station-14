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
