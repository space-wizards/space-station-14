using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Repairable;

public abstract partial class SharedRepairableSystem : EntitySystem
{
    [Serializable, NetSerializable]
    protected sealed partial class RepairFinishedEvent : SimpleDoAfterEvent
    {
    }
}

[ByRefEvent]
public record struct CanRepaireEvent(Entity<RepairableComponent> Ent, EntityUid User)
{
    public bool Cancelled = false;
    public string Message = "You can't repair this!";
}