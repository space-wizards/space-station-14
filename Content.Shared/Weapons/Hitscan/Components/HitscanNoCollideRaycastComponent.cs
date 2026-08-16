using Content.Shared.Physics;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// Will shot a raycast that will travel through all entites within <see cref="Range"/>.
/// Will *not* stop when the first entity is hit.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanNoCollideRaycastComponent : Component
{
    /// <summary>
    /// Range of the hitscan, will stop if canceled (E.g reflected by an energy sword)
    /// </summary>
    [DataField]
    public float Range = 10.0f;

    /// <summary>
    /// The collision mask the hitscan ray uses to collide with other objects. See the enum for more information
    /// </summary>
    [DataField]
    public CollisionGroup CollisionMask = CollisionGroup.AllMask;
}
