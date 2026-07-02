using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// Indicates that the hitscan laser is a radioactive one.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RadioactiveRayComponent : Component
{
    /// <summary>
    /// The conversion of rads per second to percent damage decrease.
    /// </summary>
    /// <example>
    /// A value of 20 would mean that 1 rad per second of resistance would map to -20% damage.
    /// </example>
    [DataField]
    public float RadsPerSecondToPercentDamageDecrease = 10.0f;
}
