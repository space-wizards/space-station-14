using Content.Shared.Physics;
using Content.Shared.Whitelist;

namespace Content.Server.Explosion.Components.OnTrigger;

/// <summary>
/// Generates a gravity pulse/repulse using the RepulseAttractComponent when the entity is triggered
/// </summary>
[RegisterComponent]
public sealed partial class RepulseAttractOnTriggerComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Speed = 0;

    [DataField, AutoNetworkedField]
    public float Range = 0;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField, AutoNetworkedField]
    public CollisionGroup CollisionMask = CollisionGroup.GhostImpassable;
}
