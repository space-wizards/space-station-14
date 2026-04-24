using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.Organs;

[RegisterComponent, NetworkedComponent]
public sealed partial class DamageOnInhaledAirOrganComponent : Component
{
    /// <summary>
    /// The coefficient for how much damage is taken when the air temperature is below the body <see cref="TemperatureComponent"/>'s ColdDamageThreshold
    /// </summary>
    [DataField(required: true)]
    public float ColdCoefficient;

    /// <summary>
    /// The constant for how much damage is taken when the air temperature is below the body <see cref="TemperatureComponent"/>'s ColdDamageThreshold
    /// </summary>
    [DataField(required: true)]
    public float ColdConstant;

    /// <summary>
    /// The coefficient for how much damage is taken when the air temperature is below the body <see cref="TemperatureComponent"/>'s HeatDamageThreshold
    /// </summary>
    [DataField(required: true)]
    public float HeatCoefficient;

    /// <summary>
    /// The constant for how much damage is taken when the air temperature is below the body <see cref="TemperatureComponent"/>'s HeatDamageThreshold
    /// </summary>
    [DataField(required: true)]
    public float HeatConstant;
}
