using Content.Shared.Physics;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.RepulseAttract;

/// <summary>
///     Used to repulse or attract entities away from the entity this is on
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(RepulseAttractSystem))]
public sealed partial class RepulseAttractComponent : Component
{
    /// <summary>
    ///     How fast should the Repulsion/Attraction be?
    ///     A positive value will repulse objects, a negative value will attract
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Speed = 5.0f;

    /// <summary>
    ///     How close do the entities need to be?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 5.0f;

    /// <summary>
    ///     What kind of entities should this effect apply to?
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    ///     What collision layers should be excluded?
    ///     The default excludes ghost mobs, revenants, the AI camera etc.
    /// </summary>
    [DataField, AutoNetworkedField]
    public CollisionGroup CollisionMask = CollisionGroup.GhostImpassable;
}
