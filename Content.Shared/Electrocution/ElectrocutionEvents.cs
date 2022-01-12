using Content.Shared.Inventory;
using Robust.Shared.GameObjects;

namespace Content.Shared.Electrocution
{
    public class ElectrocutionAttemptEvent : CancellableEntityEventArgs, IInventoryRelayEvent
    {
        public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;

        public readonly EntityUid TargetUid;
        public readonly EntityUid? SourceUid;
        public float SiemensCoefficient = 1f;

        public ElectrocutionAttemptEvent(EntityUid targetUid, EntityUid? sourceUid, float siemensCoefficient)
        {
            TargetUid = targetUid;
            SourceUid = sourceUid;
            SiemensCoefficient = siemensCoefficient;
        }
    }

    public class ElectrocutedEvent : EntityEventArgs
    {
        public readonly EntityUid TargetUid;
        public readonly EntityUid? SourceUid;
        public readonly float SiemensCoefficient;

        public ElectrocutedEvent(EntityUid targetUid, EntityUid? sourceUid, float siemensCoefficient)
        {
            TargetUid = targetUid;
            SourceUid = sourceUid;
            SiemensCoefficient = siemensCoefficient;
        }
    }
}
