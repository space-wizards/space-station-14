using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

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
    public int Age = 1;

    /// <summary>
    /// Number of growth cycles to skip due to poor conditions.
    /// </summary>
    [DataField]
    public int SkipAging;

    /// <summary>
    /// Whether the plant is dead.
    /// </summary>
    [DataField]
    public bool Dead = false;

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
    public float Health = 100;

    /// <summary>
    /// Game time for the next plant reagent update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// Number of missing gases required for plant growth.
    /// </summary>
    [DataField]
    public int MissingGas;

    /// <summary>
    /// Time between plant growth updates.
    /// </summary>
    [DataField]
    public TimeSpan CycleDelay = TimeSpan.FromSeconds(15f);

    /// <summary>
    /// Game time when the plant last did a growth update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastCycle = TimeSpan.Zero;

    /// <summary>
    /// Set to true to force an update cycle regardless of timing.
    /// </summary>
    [DataField]
    public bool ForceUpdate;

    /// <summary>
    /// Current pest level in the plant (0-10).
    /// </summary>
    [DataField]
    public float PestLevel;

    /// <summary>
    /// Current toxin level in the plant (0-100).
    /// </summary>
    [DataField]
    public float Toxins;

    /// <summary>
    /// True if the plant is losing health due to too high/low temperature.
    /// </summary>
    [DataField]
    public bool ImproperHeat;

    /// <summary>
    /// True if the plant is losing health due to too high/low pressure.
    /// </summary>
    [DataField]
    public bool ImproperPressure;
}
