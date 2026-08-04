using Content.Shared.Inventory;

namespace Content.Shared.Electrocution;

public sealed class ElectrocutionAttemptEvent(
    EntityUid targetUid,
    EntityUid? sourceUid,
    float siemensCoefficient,
    SlotFlags targetSlots)
    : CancellableEntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = targetSlots;

    public readonly EntityUid TargetUid = targetUid;
    public readonly EntityUid? SourceUid = sourceUid;
    public float SiemensCoefficient = siemensCoefficient;
}

public sealed class ElectrocutedEvent(EntityUid targetUid, EntityUid? sourceUid, float siemensCoefficient)
    : EntityEventArgs
{
    public readonly EntityUid TargetUid = targetUid;
    public readonly EntityUid? SourceUid = sourceUid;
    public readonly float SiemensCoefficient = siemensCoefficient;
}
