using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;

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
    public HumanoidMarkingModifierState(
        MarkingSet markingSet,
        string species,
        Sex sex,
        Color skinColor,
        Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> customBaseLayers
    )
    {
        MarkingSet = markingSet;
        Species = species;
        Sex = sex;
        SkinColor = skinColor;
        CustomBaseLayers = customBaseLayers;
    }

    public MarkingSet MarkingSet { get; }
    public string Species { get; }
    public Sex Sex { get; }
    public Color SkinColor { get; }
    public Color EyeColor { get; }
    public Color? HairColor { get; }
    public Color? FacialHairColor { get; }
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers { get; }
}
