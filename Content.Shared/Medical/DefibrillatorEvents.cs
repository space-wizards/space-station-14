using Content.Shared.Inventory;

namespace Content.Shared.Medical;

[ByRefEvent]
public readonly record struct TargetDefibrillatedEvent(EntityUid User, Entity<DefibrillatorComponent> Defibrillator);

public abstract class BeforeDefibrillatorZapsEvent : CancellableEntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;
    public EntityUid EntityUsingDefib;
    public readonly EntityUid Defib;
    public EntityUid DefibTarget;

    public BeforeDefibrillatorZapsEvent(EntityUid entityUsingDefib, EntityUid defib, EntityUid defibTarget)
    {
        EntityUsingDefib = entityUsingDefib;
        Defib = defib;
        DefibTarget = defibTarget;
    }
}

/// <summary>
///     This event is raised on the user using the defibrillator before is actually zaps someone.
///     The event is triggered on the user and all their clothing.
/// </summary>
public sealed class SelfBeforeDefibrillatorZapsEvent : BeforeDefibrillatorZapsEvent
{
    public SelfBeforeDefibrillatorZapsEvent(EntityUid entityUsingDefib, EntityUid defib, EntityUid defibtarget) : base(entityUsingDefib, defib, defibtarget) { }
}

/// <summary>
///     This event is raised on the target before it gets zapped with the defibrillator.
///     The event is triggered on the target itself and all its clothing.
/// </summary>
public sealed class TargetBeforeDefibrillatorZapsEvent : BeforeDefibrillatorZapsEvent
{
    public TargetBeforeDefibrillatorZapsEvent(EntityUid entityUsingDefib, EntityUid defib, EntityUid defibtarget) : base(entityUsingDefib, defib, defibtarget) { }
}
