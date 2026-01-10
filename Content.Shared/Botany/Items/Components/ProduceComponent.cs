using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Items.Components;

/// <summary>
/// Produce-related data for plant and plant growth cycle.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BotanySystem))]
public sealed partial class ProduceComponent : Component
{
    /// <summary>
    /// Name of a base plant prototype to spawn when extracting seeds.
    /// </summary>
    [DataField("plantId"), AutoNetworkedField]
    public EntProtoId? PlantProtoId;

    /// <summary>
    /// Serialized snapshot of plant components.
    /// Used to create a <see cref="SeedComponent"/> when this produce has its seeds extracted.
    /// </summary>
    [DataField]
    public ComponentRegistry? PlantData;

    /// <summary>
    /// Name of the solution container that holds the produce's contents.
    /// </summary>
    [DataField("targetSolution")]
    public string SolutionName { get; set; } = "food";

    /// <summary>
    /// Divider for the nutrient bonus when composting this produce.
    /// </summary>
    [DataField]
    public float NutrientDivider = 2.5f;
}
