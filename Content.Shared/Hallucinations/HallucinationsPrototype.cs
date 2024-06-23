using Robust.Shared.Prototypes;

namespace Content.Shared.Hallucinations;

/// <summary>
///     Packs of entities that can become a hallucination
/// </summary>

[Prototype("hallucinationsPack")]
public sealed partial class HallucinationsPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     List of prototypes that are spawned as a hallucination.
    /// </summary>
    [DataField("entities")]
    public List<EntProtoId> Entities = new();

    /// <summary>
    /// How far from humanoid can appear hallucination
    /// </summary>
    [DataField("spawnRange")]
    public float Range = 7f;

    /// <summary>
    /// How often (in seconds) hallucinations spawned
    /// </summary>
    [DataField("spawnRate")]
    public float SpawnRate = 15f;

    /// <summary>
    /// Minimum spawn chance per humanoid
    /// </summary>
    [DataField("minChance")]
    public float MinChance = 0.8f;

    /// <summary>
    /// Max spawn chance per humanoid
    /// </summary>
    [DataField("maxChance")]
    public float MaxChance = 0.8f;

    /// <summary>
    /// How much chance increased per spawn
    /// </summary>
    [DataField("increasedPerSpawn")]
    public float IncreaseChance = 0.1f;

    /// <summary>
    /// Max spawned hallucinations count for one spawn
    /// </summary>
    [DataField("maxSpawns")]
    public int MaxSpawns = 3;
}
