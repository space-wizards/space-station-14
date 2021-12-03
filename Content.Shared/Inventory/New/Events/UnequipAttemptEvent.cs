using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory.New.Events;

public class UnequipAttemptEventBase : CancellableEntityEventArgs
{
    /// <summary>
    /// The entity equipping.
    /// </summary>
    public readonly EntityUid Equipee;

    /// <summary>
    /// The entity to be equipped.
    /// </summary>
    public readonly EntityUid Equipment;

    /// <summary>
    /// If cancelling and wanting to provide a custom reason, use this field. Not that this expects a loc-id.
    /// </summary>
    public string? Reason;

    public UnequipAttemptEventBase(EntityUid equipee, EntityUid equipment)
    {
        Equipee = equipee;
        Equipment = equipment;
    }
}

public class BeingUnequippedAttemptEvent : UnequipAttemptEventBase
{
    public BeingUnequippedAttemptEvent(EntityUid equipee, EntityUid equipment) : base(equipee, equipment)
    {
    }
}

public class IsUnequippingAttemptEvent : UnequipAttemptEventBase
{
    public IsUnequippingAttemptEvent(EntityUid equipee, EntityUid equipment) : base(equipee, equipment)
    {
    }
}
