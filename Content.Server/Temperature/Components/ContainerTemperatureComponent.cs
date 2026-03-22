using Content.Shared.Temperature.Components;

namespace Content.Server.Temperature.Components;

/// <summary>
/// Overrides the <see cref="TemperatureDamageComponent"/> thresholds for an entity inserted into this container.
/// Used for cryogenics typically.
/// </summary>
[RegisterComponent]
public sealed partial class ContainerTemperatureComponent : Component
{
    /// <summary>
    /// Overrides the <see cref="TemperatureDamageComponent.HeatDamageThreshold"/> for an entity inserted into this container.
    /// </summary>
    [DataField]
    public float? HeatDamageThreshold;

    /// <summary>
    /// Overrides the <see cref="TemperatureDamageComponent.ColdDamageThreshold"/> for an entity inserted into this container.
    /// </summary>
    [DataField]
    public float? ColdDamageThreshold;
}
