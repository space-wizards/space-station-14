namespace Content.Server.Botany.Components;

/// <summary> Aggregate for general plant information and rare quirks. </summary>
[RegisterComponent]
[DataDefinition]
public sealed partial class PlantTraitsComponent : Component
{
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
    /// A scalar for sprite size and chemical quantity on the produce. Caps at 100.
    /// </summary>
    [DataField]
    public float Potency = 1f;

    /// TODO: The logic for these fields is quite hardcoded.
    /// They require a separate component and a system that will use events or APIs from other growth systems.

    /// <summary>
    /// If true, produce can't be put into the seed maker.
    /// </summary>
    [DataField]
    public bool Seedless = false;

    /// <summary>
    /// If true, a sharp tool is required to harvest this plant.
    /// </summary>
    [DataField]
    public bool Ligneous = false;

    /// <summary>
    /// If true, the plant can scream when harvested.
    /// </summary>
    [DataField]
    public bool CanScream = false;

    /// <summary>
    /// If true, the plant can turn into kudzu.
    /// </summary>
    [DataField]
    public bool TurnIntoKudzu = false;

    /// <summary>
    /// If false, rapidly decrease health while growing. Adds a bit of challenge to keep mutated plants alive via Unviable's frequency.
    /// </summary>
    [DataField]
    public bool Viable = true;
}
