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
