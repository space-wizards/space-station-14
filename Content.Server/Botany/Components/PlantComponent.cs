using Content.Shared.Database;
using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Botany.Components;

/// <summary>
/// Component for storing core plant data.
/// </summary>
[RegisterComponent]
[DataDefinition]
public sealed partial class PlantComponent : Component
{
    /// <summary>
    /// The noun for this type of seeds. E.g. for fungi this should probably be "spores" instead of "seeds". Also
    /// used to determine the name of seed packets.
    /// </summary>
    [DataField]
    public string Noun { get; private set; } = "";

    /// <summary>
    /// Name displayed when examining the hydroponics tray. Describes the actual plant, not the seed itself.
    /// </summary>
    [DataField]
    public string DisplayName { get; private set; } = "";

    /// <summary>
    /// The entity prototype that is spawned when this type of seed is extracted from produce using a seed extractor.
    /// </summary>
    [DataField]
    public EntProtoId PacketPrototype = "SeedBase";

    /// <summary>
    /// The entity prototypes that are spawned when this type of seed is harvested.
    /// </summary>
    [DataField]
    public List<EntProtoId> ProductPrototypes = [];

    [DataField(required: true)]
    public ResPath PlantRsi { get; set; } = default!;

    /// <summary>
    /// Log impact for harvest operations.
    /// </summary>
    [DataField]
    public LogImpact? HarvestLogImpact;

    /// <summary>
    /// Log impact for plant operations.
    /// </summary>
    [DataField]
    public LogImpact? PlantLogImpact;

    /// <summary>
    /// The mutation effects that have been applied to this plant.
    /// </summary>
    [DataField]
    public List<RandomPlantMutation> Mutations { get; set; } = [];

    /// <summary>
    /// The plant prototypes this plant may mutate into when prompted to.
    /// </summary>
    [DataField]
    public List<EntProtoId> MutationPrototypes = [];

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
