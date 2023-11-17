using Content.Shared.Damage;
using Content.Shared.Inventory;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Explosion;

/// <summary>
///     Raised directed at an entity to determine its explosion resistance, probably right before it is about to be
///     damaged by one.
/// </summary>
[ByRefEvent]
public record struct GetExplosionResistanceEvent(string ExplosionPrototype) : IInventoryRelayEvent
{
    /// <summary>
    ///     A coefficient applied to overall explosive damage.
    /// </summary>
    public float DamageCoefficient = 1;

    public readonly string ExplosionPrototype = ExplosionPrototype;

    SlotFlags IInventoryRelayEvent.TargetSlots =>  ~SlotFlags.POCKET;
}

/// <summary>
/// Raised on an entity caught in an explosion to do damage to it any potentially add contents to also explode.
/// Various systems add to contents such as inventory and storage.
/// This is recursive so a matchbox in a backpack in a player's inventory will be handled.
/// </summary>
[ByRefEvent]
public record struct ExplodedEvent
{
    public readonly DamageSpecifier? Damage;

    public readonly float ThrowForce;

    /// <summary>
    /// ID of the explosion prototype.
    /// </summary>
    public readonly string Id;

    /// <summary>
    /// Transform of the entity in the explosion the event was raised on.
    /// </summary>
    public readonly TransformComponent? Xform;

    /// <summary>
    /// Entities considered contents for recursive explo
    /// Use <c>Add</c> or <c>AddRange</c> to add entities to be damaged.
    /// </summary>
    public List<EntityUid> Contents = new();

    public ExplodedEvent(DamageSpecifier? damage, float throwForce, string id, TransformComponent? xform)
    {
        Damage = damage;
        ThrowForce = throwForce;
        Id = id;
        Xform = xform;
    }
}
