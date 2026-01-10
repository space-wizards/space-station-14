using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Basic parameters for plant growth.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(BasicGrowthSystem))]
public sealed partial class BasicGrowthComponent : Component
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
