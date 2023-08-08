using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Repairable;

public abstract class SharedRepairableSystem : EntitySystem
{
    [Serializable, NetSerializable]
    protected sealed class RepairFinishedEvent : SimpleDoAfterEvent
    {
    }
}

