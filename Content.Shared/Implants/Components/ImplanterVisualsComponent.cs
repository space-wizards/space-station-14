using Content.Shared.Storage.Components;
using Robust.Shared.Serialization;

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
    public readonly IReadOnlyDictionary<string, Color> QueuedEntities;

    public ColorLayerData()
    {
        QueuedEntities = new Dictionary<string, Color>();
    }

    public ColorLayerData(IReadOnlyDictionary<string, Color> other)
    {
        QueuedEntities = other;
    }

    public object Clone()
    {
        // QueuedEntities should never be getting modified after this object is created.
        return this;
    }
}
