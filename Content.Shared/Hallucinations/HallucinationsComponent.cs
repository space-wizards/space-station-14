using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Hallucinations;

[RegisterComponent]
public sealed partial class HallucinationsComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextSecond = TimeSpan.Zero;

    /// <summary>
    /// How far from humanoid can appear hallucination
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Range = 7f;

    /// <summary>
    /// How often (in seconds) hallucinations spawned
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SpawnRate = 15f;

    /// <summary>
    /// Minimum spawn chance per humanoid
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinChance = 0.1f;

    /// <summary>
    /// Max spawn chance per humanoid
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxChance = 0.8f;

    /// <summary>
    /// How much chance increased per spawn
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float IncreaseChance = 0.1f;

    /// <summary>
    /// Max spawned hallucinations count for one spawn
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxSpawns = 3;

    /// <summary>
    /// How much entities already spawned
    /// </summary>
    public int SpawnedCount = 0;

    /// <summary>
    /// Current spawn chance
    /// </summary>
    public float CurChance = 0.1f;

    /// <summary>
    ///     List of prototypes that are spawned as a hallucination.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<EntProtoId> Spawns = new();

    /// <summary>
    /// Hallucinations pack proto
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public HallucinationsPrototype? Proto;

    /// <summary>
    /// Currently selected for hallucinations layer
    /// </summary>
    public int Layer = 50;
}
