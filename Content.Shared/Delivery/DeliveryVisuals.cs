using Robust.Shared.Serialization;

namespace Content.Shared.Delivery;

[Serializable, NetSerializable]
public enum DeliveryVisuals : byte
{
    IsLocked,
    IsTrash,
    IsBroken,
    IsFragile,
    IsBomb,
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
public enum DeliveryBombState : byte
{
    Off,
    Inactive,
    Primed,
}

[Serializable, NetSerializable]
public enum DeliverySpawnerVisuals : byte
{
    Contents,
}
