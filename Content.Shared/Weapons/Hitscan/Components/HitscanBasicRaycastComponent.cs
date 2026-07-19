using Content.Shared.Physics;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// A basic raycast system that will shoot in a straight line when triggered.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanBasicRaycastComponent : Component
{
    /// <summary>
    /// Maximum distance the raycast will travel before giving up. Reflections will reset the distance traveled
    /// </summary>
    [DataField]
    public float MaxDistance = 20.0f;

    /// <summary>
    /// The collision mask the hitscan ray uses to collide with other objects. See the enum for more information
    /// </summary>
    [DataField]
    public CollisionGroup CollisionMask = CollisionGroup.Opaque;
}
