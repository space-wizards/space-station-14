namespace Content.Server.Botany.Components;

/// <summary>
/// Runtime plant lifecycle data. This component is attached to the plant entity.
/// </summary>
[RegisterComponent]
public sealed partial class PlantHolderComponent : Component
{
    /// <summary>
    /// Current age of the plant in growth cycles.
    /// </summary>
    [DataField]
    public int Age;

    /// <summary>
    /// Number of growth cycles to skip due to poor conditions.
    /// </summary>
    [DataField]
    public int SkipAging;

    /// <summary>
    /// Whether the plant is dead.
    /// </summary>
    [DataField]
    public bool Dead;

    /// <summary>
    /// Set to true if this plant has been clipped by seed clippers. Used to prevent a single plant
    /// from repeatedly being clipped.
    /// </summary>
    [DataField]
    public bool Sampled;

    /// <summary>
    /// Multiplier for the number of entities produced at harvest.
    /// </summary>
    [DataField]
    public int YieldMod = 1;

    /// <summary>
    /// Multiplier for mutation chance and severity.
    /// </summary>
    [DataField]
    public float MutationMod = 1f;

    /// <summary>
    /// Current mutation level (0-100).
    /// </summary>
    [DataField]
    public float MutationLevel;

    /// <summary>
    /// Current health of the plant (0 to seed endurance).
    /// </summary>
    [DataField]
    public float Health;
}
