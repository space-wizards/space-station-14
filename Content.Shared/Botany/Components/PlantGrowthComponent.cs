using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Component for basic parameters for plant growth.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(PlantGrowthSystem))]
public sealed partial class PlantGrowthComponent : Component
{
    /// <summary>
    /// Amount of water consumed per growth tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WaterConsumption = 0.5f;

    /// <summary>
    /// Amount of nutrients consumed per growth tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float NutrientConsumption = 0.75f;
}
