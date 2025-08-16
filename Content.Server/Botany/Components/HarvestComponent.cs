using Content.Server.Botany.Systems;

namespace Content.Server.Botany.Components;

[RegisterComponent]
[DataDefinition]
public sealed partial class HarvestComponent : PlantGrowthComponent
{
    /// <summary>
    /// Harvest options are NoRepeat(plant is removed on harvest), Repeat(Plant makes produce every Production ticks),
    /// and SelfHarvest (Repeat, plus produce is dropped on the ground near the plant automatically)
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
    public float LastHarvestTime = 0f;
}
