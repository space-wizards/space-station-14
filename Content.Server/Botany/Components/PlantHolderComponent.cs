using Content.Server.Botany.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Botany.Components;

/// <summary>
/// Runtime plant lifecycle data. This component is attached to the plant entity.
/// </summary>
[RegisterComponent]
[Access(typeof(PlantHolderSystem), typeof(PlantSystem))]
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
    [ViewVariables]
    public bool Dead;

    /// <summary>
    /// Multiplier for the number of entities produced at harvest.
    /// </summary>
    [DataField]
    public int YieldMod = 1;

    [DataField]
    public int MaxYieldMod = 2;

    /// <summary>
    /// Multiplier for mutation chance and severity.
    /// </summary>
    [DataField]
    public float MutationMod = 1f;

    [DataField]
    public float MaxMutationMod = 3f;

    /// <summary>
    /// Current mutation level.
    /// </summary>
    [DataField]
    public float MutationLevel;

    public float MaxMutationLevel = 100f;

    /// <summary>
    /// Current health of the plant (0 to seed endurance).
    /// </summary>
    [DataField]
    public float Health = 100f;

    /// <summary>
    /// Game time for the next plant reagent update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;

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
    /// Current pest level in the plant.
    /// </summary>
    [DataField]
    public float PestLevel;

    [DataField]
    public float MaxPestLevel = 10f;

    /// <summary>
    /// Current toxin level in the plant.
    /// </summary>
    [DataField]
    public float Toxins;

    [DataField]
    public float MaxToxins = 100f;

    /// <summary>
    /// True if the plant is losing health due to too high/low temperature.
    /// </summary>
    [DataField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool ImproperHeat;

    /// <summary>
    /// True if the plant is losing health due to too high/low pressure.
    /// </summary>
    [DataField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool ImproperPressure;

    /// <summary>
    /// True if the plant is missing gases required for growth.
    /// </summary>
    [DataField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool MissingGas;
}
