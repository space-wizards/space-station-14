namespace Content.Server.Botany.Components;

/// <summary> 
/// Data for plant harvesting process. 
/// </summary>
[RegisterComponent]
[DataDefinition]
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
    public bool ReadyForHarvest = false;

    /// <summary>
    /// The last time this plant was harvested.
    /// </summary>
    [ViewVariables]
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
