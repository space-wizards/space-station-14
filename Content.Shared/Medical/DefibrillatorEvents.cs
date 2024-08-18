namespace Content.Shared.Medical;
using Content.Shared.Inventory;

[ByRefEvent]
public readonly record struct TargetDefibrillatedEvent(EntityUid User, Entity<DefibrillatorComponent> Defibrillator);

public abstract partial class BeforeDefibrillatorZaps : CancellableEntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;
    public EntityUid EntityUsingDefib;
    public readonly EntityUid Defib;
    public EntityUid DefibTarget;

    public BeforeDefibrillatorZaps(EntityUid entityUsingDefib, EntityUid defib, EntityUid defibTarget)
    {
        EntityUsingDefib = entityUsingDefib;
        Defib = defib;
        DefibTarget = defibTarget;
    }
}

/// <summary>
///     This event is raised on the user using the hypospray before the hypospray is injected.
///     The event is triggered on the user and all their clothing.
/// </summary>
public sealed class SelfBeforeDefibrillatorZaps : BeforeDefibrillatorZaps
{
    public SelfBeforeDefibrillatorZaps(EntityUid entityUsingDefib, EntityUid defib, EntityUid defibtarget) : base(entityUsingDefib, defib, defibtarget) { }
}

/// <summary>
///     This event is raised on the target before the hypospray is injected.
///     The event is triggered on the target itself and all its clothing.
/// </summary>
public sealed class TargetBeforeDefibrillatorZaps : BeforeDefibrillatorZaps
{
    public TargetBeforeDefibrillatorZaps(EntityUid entityUsingDefib, EntityUid defib, EntityUid defibtarget) : base(entityUsingDefib, defib, defibtarget) { }
}
