using Content.Shared.Random;

namespace Content.Server.Botany.Components;

/// <summary>
/// Component for storing plant growth data.
/// </summary>
[RegisterComponent]
[DataDefinition]
public sealed partial class PlantComponent : Component
{
    /// <summary>
    /// The mutation effects that have been applied to this plant.
    /// </summary>
    [DataField]
    public List<RandomPlantMutation> Mutations { get; set; } = [];

    /// <summary>
    /// The plant's max health.
    /// </summary>
    [DataField]
    public float Endurance = 100f;

    /// <summary>
    /// How many produce are created on harvest.
    /// </summary>
    [DataField]
    public int Yield;

    /// <summary>
    /// The number of growth ticks this plant can be alive for. Plants take high damage levels when Age > Lifespan.
    /// </summary>
    [DataField]
    public float Lifespan;

    /// <summary>
    /// The number of growth ticks it takes for a plant to reach its final growth stage.
    /// </summary>
    [DataField]
    public float Maturation;

    /// <summary>
    /// The number of growth ticks it takes for a plant to be (re-)harvestable. Shouldn't be lower than Maturation.
    /// </summary>
    [DataField]
    public float Production;

    /// <summary>
    /// How many different sprites appear before the plant is fully grown.
    /// </summary>
    [DataField]
    public int GrowthStages = 6;

    /// <summary>
    /// A scalar for sprite size and chemical solution volume in the produce. Caps at 100.
    /// </summary>
    [DataField]
    public float Potency = 1f;
}
