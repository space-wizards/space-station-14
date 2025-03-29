using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Marker for temperature thresholds starting which entity should take damage (for heating and chilling entity respectively).
/// Thresholds using this component are calculated by recursively looking through <see cref="TransformComponent.ParentUid"/> for greater value
/// of threshold (min for cold, max for heat) through all entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ContainerTemperatureDamageThresholdsComponent : Component
{
    /// <summary>
    /// Threshold for temperature value, if temperature of entity is greater than this - it should start take heat damage.
    /// Can be overriden by any parent entity wth this component and greater value of <see cref="HeatDamageThreshold"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? HeatDamageThreshold;

    /// <summary>
    /// Threshold for temperature value, if temperature of entity is lesser than this - it should start take heat damage.
    /// Can be overriden by any parent entity wth this component and lesser value of <see cref="ColdDamageThreshold"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? ColdDamageThreshold;
}
