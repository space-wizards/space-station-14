using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// Provides basic visuals for hitscan weapons - works with <see cref="HitscanBasicRaycastComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanBasicVisualsComponent : Component
{
    [DataField]
    public SpriteSpecifier? MuzzleFlash;

    [DataField]
    public SpriteSpecifier? TravelFlash;

    [DataField]
    public SpriteSpecifier? ImpactFlash;
}
