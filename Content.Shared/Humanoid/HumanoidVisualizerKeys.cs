using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid;

[Serializable, NetSerializable]
public enum HumanoidVisualizerKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class HumanoidVisualizerData : ICloneable
{
    public HumanoidVisualizerData(string species, Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> customBaseLayerInfo, Color skinColor, List<HumanoidVisualLayers> layerVisibility, List<Marking> markings)
    {
        Species = species;
        CustomBaseLayerInfo = customBaseLayerInfo;
        SkinColor = skinColor;
        LayerVisibility = layerVisibility;
        Markings = markings;
    }

    public string Species { get; }
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayerInfo { get; }
    public Color SkinColor { get; }
    public List<HumanoidVisualLayers> LayerVisibility { get; }
    public List<Marking> Markings { get; }

    public object Clone()
    {
        return new HumanoidVisualizerData(Species, new(CustomBaseLayerInfo), SkinColor, new(LayerVisibility), new(Markings));
    }
}
