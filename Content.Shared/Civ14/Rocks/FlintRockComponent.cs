using Robust.Shared.Prototypes;

namespace Content.Shared.Rocks;

[RegisterComponent]
public sealed partial class FlintRockComponent : Component
{
    /// <summary>
    /// Actual available flints amount
    /// </summary>
    [DataField("currentFlints")]
    public int CurrentFlints = 0;

    /// <summary>
    /// Maximum flint amount on rock
    /// </summary>
    [DataField("maxFlints")]
    public int MaxFlints = 2;

    /// <summary>
    /// Time in hours to create a new flint
    /// </summary>
    [DataField("regenerationTime")]
    public float RegenerationTime = 1.0f; // 1 hora

    /// <summary>
    /// Last time that it regenerated a flint
    /// </summary>
    [DataField("lastRegenerationTime")]
    public TimeSpan LastRegenerationTime = TimeSpan.Zero;

    /// <summary>
    /// The time taken to collect flint from a rock
    /// </summary>
    [DataField("collectionTime")]
    public float CollectionTime { get; set; } = 5.0f;
}