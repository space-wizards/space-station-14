using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
///     When activated artifact will spawn an entity from prototype.
///     It could be an angry mob or some random item.
/// </summary>
[RegisterComponent]
public sealed class SpawnArtifactComponent : Component
{
    /// <summary>
    /// The list of possible prototypes to spawn that it picks from.
    /// </summary>
    [DataField("possiblePrototypes", customTypeSerializer:typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> PossiblePrototypes = new();

    /// <summary>
    /// The prototype it selected to spawn.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("prototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? Prototype;

    /// <summary>
    /// The range around the artifact that it will spawn the entity
    /// </summary>
    [DataField("range")]
    public float Range = 0.5f;

    /// <summary>
    /// The maximum number of times the spawn will occur
    /// </summary>
    [DataField("maxSpawns")]
    public int MaxSpawns = 20;

    /// <summary>
    /// Whether or not the artifact spawns the same entity every time
    /// or picks through the list each time.
    /// </summary>
    [DataField("consistentSpawn")]
    public bool ConsistentSpawn = true;
}
