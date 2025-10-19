using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// Provides basic visuals for hitscan weapons - works with <see cref="HitscanBasicRaycastComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanBasicVisualsComponent : Component
{
    /// <summary>
    /// The muzzle flash from the hitscan weapon.
    /// </summary>
    [DataField]
    public SpriteSpecifier? MuzzleFlash;

    /// <summary>
    /// The "travel" sprite, this gets repeated until it hits the target.
    /// </summary>
    [DataField]
    public SpriteSpecifier? TravelFlash;

    /// <summary>
    /// The sprite that gets shown on the impact of the laser.
    /// </summary>
    [DataField]
    public SpriteSpecifier? ImpactFlash;
}
