using Content.Server.Temperature.Systems;
using Content.Server.Temperature.Components; 
using Content.Shared.Temperature;

namespace Content.Server.Temperature.Components;

/// <summary>
/// Put this component on a projectile that you would like to change the temperature on whatever it hits.
/// </summary>
[RegisterComponent, Access(typeof(TemperatureSystem))]
public sealed partial class ChangeTemperatureOnCollideComponent : Component
{
    /// <summary>
    /// The amount it changes the target's temperature by. In Joules.
    /// </summary>
    [DataField]
    public float Heat = 0f;

    /// <summary>
    /// If this heat change ignores heat resistance or not.
    /// </summary>
    [DataField]
    public bool IgnoreHeatResistance = true;
}