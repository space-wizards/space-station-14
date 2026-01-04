using Content.Server.Botany.Systems;

namespace Content.Server.Botany.Components;

/// <summary>
/// Data for plant harvesting process.
/// </summary>
[RegisterComponent]
[DataDefinition]
[Access(typeof(PlantHarvestSystem))]
public sealed partial class PlantHarvestComponent : Component
{
    /// <summary>
    /// Harvest repeat type.
    /// </summary>
    [DataField]
    public HarvestType HarvestRepeat = HarvestType.NoRepeat;

    /// <summary>
    /// Whether the plant is currently ready for harvest.
    /// </summary>
    [ViewVariables]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool ReadyForHarvest = false;

    /// <summary>
    /// The age of the plant when last harvested.
    /// </summary>
    [ViewVariables]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public int LastHarvest = 0;
}

/// <summary>
/// Harvest options for plants.
/// </summary>
public enum HarvestType
{
    /// <summary>
    /// Plant is removed on harvest.
    /// </summary>
    NoRepeat,

    /// <summary>
    /// Plant makes produce every Production ticks.
    /// </summary>
    Repeat,

    /// <summary>
    /// Repeat, plus produce is dropped on the ground near the plant automatically.
    /// </summary>
    SelfHarvest
}
