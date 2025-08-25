using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid;

[Serializable, NetSerializable]
public enum HumanoidMarkingModifierKey { Key }

[Serializable, NetSerializable]
public sealed class HumanoidMarkingModifierMarkingSetMessage(MarkingSet set)
    : BoundUserInterfaceMessage
{
    public MarkingSet MarkingSet { get; } = set;
}

[Serializable, NetSerializable]
public sealed class HumanoidMarkingModifierBaseLayersSetMessage(HumanoidVisualLayers layer, CustomBaseLayerInfo? info)
    : BoundUserInterfaceMessage
{
    public HumanoidVisualLayers Layer { get; } = layer;
    public CustomBaseLayerInfo? Info { get; } = info;
}
