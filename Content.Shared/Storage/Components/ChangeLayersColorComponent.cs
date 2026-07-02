using Content.Shared.Storage.Components;
using Robust.Shared.Serialization;
using System.Linq;

namespace Content.Shared.Storage;

/// <summary>
/// Component used to define which sprite layers should change color based on inserted entities.
/// </summary>
/// <remarks>
/// The actual color to be applied is determined by the <see cref="ItemLayersColorComponent"/>
/// on the inserted item, and changes are applied via the <see cref="SharedItemChangeLayerColorSystem"/>.
/// </remarks>
[RegisterComponent]
public sealed partial class ChangeLayersColorComponent : Component
{
    [DataField]
    public Dictionary<string, SharedMapLayerData> MapLayers = new();

    /// <summary>
    ///     If this exists, shown layers will only consider entities in the given containers.
    /// </summary>
    [DataField]
    public HashSet<string>? ContainerWhitelist;

    /// <summary>
    ///     The list of map layer keys that are valid targets for changing in <see cref="MapLayers"/>
    ///     Can be initialized if already existing on the sprite, or inferred automatically
    /// </summary>
    [DataField]
    public List<string> SpriteLayers = new();
}

[Serializable, NetSerializable]
public sealed class ColorLayerData : ICloneable
{
    public readonly LayerColor[] LayersColors;

    public ColorLayerData()
    {
        LayersColors = Array.Empty<LayerColor>();
    }

    public ColorLayerData(IReadOnlyDictionary<string, Color> other)
    {
        LayersColors = other.Select(kvp => new LayerColor { LayerName = kvp.Key, Color = kvp.Value }).ToArray(); ;
    }

    public object Clone()
    {
        return this;
    }
}

[Serializable]
public struct LayerColor
{
    public string LayerName;
    public Color Color;
}

[Serializable, NetSerializable]
public enum LayerColorVisuals : sbyte
{
    InitLayers,
    LayerChanged,
}
