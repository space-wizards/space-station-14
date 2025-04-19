using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Marker for temperature thresholds at which an entity should start taking damage.
/// Thresholds using this component are calculated by recursively looking through <see cref="TransformComponent.ParentUid"/> for greater threshold values
/// (min for cold, max for heat) through all entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ContainerTemperatureDamageThresholdsComponent : Component
{
    /// <summary>
    /// The temperature threshold above which an entity will take heat damage.
    /// Can be overriden by any parent entity wth this component and greater value of <see cref="HeatDamageThreshold"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? HeatDamageThreshold;

    /// <summary>
    /// The temperature threshold below which an entity will take cold damage.
    /// Can be overriden by any parent entity wth this component and lesser value of <see cref="ColdDamageThreshold"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? ColdDamageThreshold;
}
