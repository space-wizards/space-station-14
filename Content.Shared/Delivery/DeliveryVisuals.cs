using Robust.Shared.Serialization;

namespace Content.Shared.Delivery;

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

[Serializable, NetSerializable]
public enum DeliverySpawnerVisuals : byte
{
    Contents,
}
