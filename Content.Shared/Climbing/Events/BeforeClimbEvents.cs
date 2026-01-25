using Content.Shared.Inventory;
using Content.Shared.Climbing.Components;

namespace Content.Shared.Climbing.Events;

public abstract partial class BeforeClimbEvent : CancellableEntityEventArgs
{
    public readonly EntityUid GettingPutOnTable;
    public readonly EntityUid PuttingOnTable;
    public readonly Entity<ClimbableComponent> BeingClimbedOn;

    public BeforeClimbEvent(EntityUid gettingPutOntable, EntityUid puttingOnTable, Entity<ClimbableComponent> beingClimbedOn)
    {
        GettingPutOnTable = gettingPutOntable;
        PuttingOnTable = puttingOnTable;
        BeingClimbedOn = beingClimbedOn;
    }
}

/// <summary>
///     This event is raised on the the person either getting put on or going on the table.
///     The event is also called on their clothing as well.
/// </summary>
public sealed class SelfBeforeClimbEvent : BeforeClimbEvent, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;
    public SelfBeforeClimbEvent(EntityUid gettingPutOntable, EntityUid puttingOnTable, Entity<ClimbableComponent> beingClimbedOn) : base(gettingPutOntable, puttingOnTable, beingClimbedOn) { }
}

/// <summary>
///     This event is raised on the thing being climbed on.
/// </summary>
public sealed class TargetBeforeClimbEvent : BeforeClimbEvent
{
    public TargetBeforeClimbEvent(EntityUid gettingPutOntable, EntityUid puttingOnTable, Entity<ClimbableComponent> beingClimbedOn) : base(gettingPutOntable, puttingOnTable, beingClimbedOn) { }
}
