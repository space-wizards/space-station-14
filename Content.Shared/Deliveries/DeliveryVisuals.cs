using Robust.Shared.Serialization;

namespace Content.Shared.Deliveries;

[Serializable, NetSerializable]
public enum DeliveryVisuals : byte
{
    IsLocked,
    IsTrash,
    IsBroken,
    IsFragile,
    IsPriority,
    IsPriorityInactive,
    JobIcon,
}
