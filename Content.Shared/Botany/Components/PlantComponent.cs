using Content.Shared.Botany.Systems;
using Content.Shared.Random;
using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Component for storing plant growth data.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(PlantSystem), typeof(MutationSystem))]
public sealed partial class PlantComponent : Component
{
    /// <summary>
    /// The mutation effects that have been applied to this plant.
    /// Server-only: mutations are applied as effects which are synced separately.
    /// </summary>
    [DataField]
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
