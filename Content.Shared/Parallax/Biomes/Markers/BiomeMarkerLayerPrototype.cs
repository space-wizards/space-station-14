using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Parallax.Biomes.Markers;

/// <summary>
/// Spawns entities inside of the specified area with the minimum specified radius.
/// </summary>
[Prototype("biomeMarkerLayer")]
public sealed partial class BiomeMarkerLayerPrototype : IBiomeMarkerLayer
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// Checks for the relevant entity for the tile before spawning. Useful for substituting walls with ore veins for example.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, EntProtoId> EntityMask { get; private set; } = new();

    /// <summary>
    /// Default prototype to spawn. If null will fall back to entity mask.
    /// </summary>
    [DataField]
    public string? Prototype { get; private set; }

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
    /// Minimum entities to spawn in one group.
    /// </summary>
    [DataField]
    public int MinGroupSize = 1;

    /// <summary>
    /// Maximum entities to spawn in one group.
    /// </summary>
    [DataField]
    public int MaxGroupSize = 1;

    /// <inheritdoc />
    [DataField("size")]
    public int Size { get; private set; } = 128;
}
