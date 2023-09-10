using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Parallax.Biomes.Markers;

/// <summary>
/// Spawns entities inside of the specified area with the minimum specified radius.
/// </summary>
[Prototype("biomeMarkerLayer")]
public sealed class BiomeMarkerLayerPrototype : IBiomeMarkerLayer
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype { get; private set; } = string.Empty;

    /// <summary>
    /// Checks for the relevant entity for the tile before spawning. Useful for substituting walls with ore veins for example.
    /// </summary>
    [DataField("entityMask", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? EntityMask { get; private set; }

    /// <summary>
    /// Minimum radius between 2 points
    /// </summary>
    [DataField("radius")]
    public float Radius = 32f;

    /// <summary>
    /// Maximum amount of group spawns
    /// </summary>
    [DataField("maxCount")]
    public int MaxCount = int.MaxValue;

    /// <summary>
    /// How many mobs to spawn in one group.
    /// </summary>
    [DataField("groupCount")]
    public int GroupCount = 1;

    /// <inheritdoc />
    [DataField("size")]
    public int Size { get; private set; } = 128;
}
