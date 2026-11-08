using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Component for storing plant growth data.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true, raiseAfterAutoHandleState: true)]
[Access(typeof(PlantSystem), typeof(PlantMutationSystem))]
public sealed partial class PlantComponent : Component
{
    /// <summary>
    /// Plant-specific overrides used when resolving mutations, indexed by mutation prototype ID.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<RandomPlantMutationPrototype>, RandomPlantMutationOverride> MutationOverrides = [];

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
    /// The number of growth ticks it takes for a plant to be (re-)harvestable.
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
