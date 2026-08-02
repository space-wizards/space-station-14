using Content.Shared.Database;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Component for storing plant data, growth, and species information.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true, raiseAfterAutoHandleState: true)]
public sealed partial class PlantComponent : Component
{
    /// <summary>
    /// The noun for this type of seeds. E.g. for fungi this should probably be "spores" instead of "seeds". Also
    /// used to determine the name of seed packets.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId Noun = "seeds-noun-seeds";

    /// <summary>
    /// Name displayed when examining the hydroponics tray. Describes the actual plant, not the seed itself.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public LocId Name;

    /// <summary>
    /// The entity prototype that is spawned when this type of seed is extracted from produce using a seed extractor.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId PacketPrototype;

    /// <summary>
    /// The plant prototypes this plant may mutate into when prompted to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId> MutationPrototypes = [];

    /// <summary>
    /// The entity prototypes that are spawned when this type of seed is harvested.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<EntProtoId> ProductPrototypes = [];

    /// <summary>
    /// Log impact for harvest operations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LogImpact? HarvestLogImpact;

    /// <summary>
    /// Log impact for plant operations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LogImpact? PlantLogImpact;

    /// <summary>
    /// The mutation effects that have been applied to this plant.
    /// Server-only: mutations are applied as effects which are synced separately.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<RandomPlantMutation> Mutations = [];

    /// <summary>
    /// The plant's max health.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Endurance = 100f;

    /// <summary>
    /// How many produce are created on harvest.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Yield;

    /// <summary>
    /// The number of growth ticks this plant can be alive for. Plants take high damage levels when Age > Lifespan.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Lifespan;

    /// <summary>
    /// Damage from old age.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float OldAgeDamage = 4f;

    /// <summary>
    /// The number of growth ticks it takes for a plant to reach its final growth stage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Maturation;

    /// <summary>
    /// The number of growth ticks it takes for a plant to be (re-)harvestable. Shouldn't be lower than <see cref="Maturation"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Production;

    /// <summary>
    /// How many different sprites appear before the plant is fully grown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int GrowthStages = 6;

    /// <summary>
    /// A scalar for sprite size and chemical solution volume in the produce. Caps at 100.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Potency = 1f;
}
