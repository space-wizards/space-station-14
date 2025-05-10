using Content.Shared.Storage.Components;
using Robust.Shared.Serialization;
using System.Linq;

namespace Content.Shared.Implants;

[RegisterComponent]
public sealed partial class ImplanterVisualsComponent : Component
{
    [DataField("mapLayers")] public Dictionary<string, SharedMapLayerData> MapLayers = new();

    /// <summary>
    ///     If this exists, shown layers will only consider entities in the given containers.
    /// </summary>
    [DataField("containerWhitelist")]
    public HashSet<string>? ContainerWhitelist;

    /// <summary>
    ///     The list of map layer keys that are valid targets for changing in <see cref="MapLayers"/>
    ///     Can be initialized if already existing on the sprite, or inferred automatically
    /// </summary>
    [DataField("spriteLayers")]
    public List<string> SpriteLayers = new();
}

[Serializable, NetSerializable]
public sealed class ColorLayerData : ICloneable
{
    public readonly LayereColor[] QueuedEntities;

    public ColorLayerData()
    {
        QueuedEntities = Array.Empty<LayereColor>();
    }

    public ColorLayerData(IReadOnlyDictionary<string, Color> other)
    {
        QueuedEntities = other.Select(kvp => new LayereColor { LayerName = kvp.Key, Color = kvp.Value }).ToArray(); ;
    }

    public object Clone()
    {
        // QueuedEntities should never be getting modified after this object is created.
        return this;
    }
}

[Serializable]
public struct LayereColor
{
    public string LayerName;
    public Color Color;
}
