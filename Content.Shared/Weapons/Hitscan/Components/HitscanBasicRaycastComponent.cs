using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// A basic raycast system that will shoot in a straight line when triggered.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanBasicRaycastComponent : Component
{
    /// <summary>
    /// Maximum distance the raycast will travel before giving up.
    /// </summary>
    [DataField]
    public float MaxDistance = 20.0f;
}
