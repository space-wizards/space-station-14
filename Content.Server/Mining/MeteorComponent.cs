using Content.Shared.Damage;

namespace Content.Server.Mining;

/// <summary>
/// This is used for meteors which hit objects, dealing damage to destroy/kill the object and dealing equal damage back to itself.
/// </summary>
[RegisterComponent]
public sealed partial class MeteorComponent : Component
{
    /// <summary>
    /// percentage distribution of damage applied. Is multiplied by the actual damage it needs to deal.
    /// </summary>
    [DataField]
    public DamageSpecifier DamageDistribution = new();

    /// <summary>
    /// A list of entities that this meteor has collided with. used to ensure no double collisions occur.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> HitList = new();
}
