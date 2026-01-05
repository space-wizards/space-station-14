using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Runtime plant lifecycle data. This component is attached to the plant entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(PlantHolderSystem), typeof(PlantSystem))]
public sealed partial class PlantHolderComponent : Component
{
    /// <summary>
    /// Current age of the plant in growth cycles.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Age = 1;

    /// <summary>
    /// Number of growth cycles to skip due to poor conditions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int SkipAging;

    /// <summary>
    /// Whether the plant is dead.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool Dead;

    /// <summary>
    /// Multiplier for the number of entities produced at harvest.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int YieldMod = 1;

    [DataField, AutoNetworkedField]
    public int MaxYieldMod = 2;

    /// <summary>
    /// Current mutation level.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MutationLevel;

    [DataField, AutoNetworkedField]
    public float MaxMutationLevel = 25f;

    /// <summary>
    /// Multiplier for mutation chance and severity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MutationMod = 1f;

    [DataField, AutoNetworkedField]
    public float MaxMutationMod = 3f;

    /// <summary>
    /// Current health of the plant (0 to seed endurance).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Health = 100f;

    /// <summary>
    /// Game time for the next plant reagent update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// Time between plant growth updates.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CycleDelay = TimeSpan.FromSeconds(1f);

    /// <summary>
    /// Game time when the plant last did a growth update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastCycle = TimeSpan.Zero;

    /// <summary>
    /// Set to true to force an update cycle regardless of timing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ForceUpdate;

    /// <summary>
    /// Current pest level in the plant.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PestLevel;

    [DataField, AutoNetworkedField]
    public float MaxPestLevel = 10f;

    /// <summary>
    /// Current toxin level in the plant.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Toxins;

    [DataField, AutoNetworkedField]
    public float MaxToxins = 100f;

    /// <summary>
    /// True if the plant is losing health due to too high/low temperature.
    /// </summary>
    [DataField, AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool ImproperHeat;

    /// <summary>
    /// True if the plant is losing health due to too high/low pressure.
    /// </summary>
    [DataField, AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool ImproperPressure;

    /// <summary>
    /// True if the plant is missing gases required for growth.
    /// </summary>
    [DataField, AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool MissingGas;
}
