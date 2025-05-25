using Robust.Shared.Serialization;

namespace Content.Shared.Delivery;

[Serializable, NetSerializable]
public enum DeliveryVisuals : byte
{
    IsLocked,
    IsTrash,
    IsBroken,
    IsFragile,
    PriorityState,
    JobIcon,
}

[Serializable, NetSerializable]
public enum DeliveryPriorityState : byte
{
    Off,
    Active,
    Inactive,
}

[Serializable, NetSerializable]
public enum DeliverySpawnerVisuals : byte
{
    Contents,
}
