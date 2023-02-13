using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;
using static Content.Shared.Humanoid.HumanoidAppearanceState;

namespace Content.Shared.Humanoid;

[Serializable, NetSerializable]
public enum HumanoidMarkingModifierKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class HumanoidMarkingModifierMarkingSetMessage : BoundUserInterfaceMessage
{
    public MarkingSet MarkingSet { get; }
    public bool ResendState { get; }

    public HumanoidMarkingModifierMarkingSetMessage(MarkingSet set, bool resendState)
    {
        MarkingSet = set;
        ResendState = resendState;
    }
}

[Serializable, NetSerializable]
public sealed class HumanoidMarkingModifierBaseLayersSetMessage : BoundUserInterfaceMessage
{
    public HumanoidMarkingModifierBaseLayersSetMessage(HumanoidVisualLayers layer, CustomBaseLayerInfo? info, bool resendState)
    {
        Layer = layer;
        Info = info;
        ResendState = resendState;
    }

    public HumanoidVisualLayers Layer { get; }
    public CustomBaseLayerInfo? Info { get; }
    public bool ResendState { get; }
}

[Serializable, NetSerializable]
public sealed class HumanoidMarkingModifierState : BoundUserInterfaceState
{
    // TODO just use the component state, remove the BUI state altogether.
    public HumanoidMarkingModifierState(MarkingSet markingSet, string species, Color skinColor, Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> customBaseLayers)
    {
        MarkingSet = markingSet;
        Species = species;
        SkinColor = skinColor;
        CustomBaseLayers = customBaseLayers;
    }

    public MarkingSet MarkingSet { get; }
    public string Species { get; }
    public Color SkinColor { get; }
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers { get; }
}
