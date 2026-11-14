using Robust.Shared.Serialization;

namespace Content.Shared.VendingMachines;

[Serializable, NetSerializable]
public enum ContrabandWireKey : byte
{
    StatusKey,
    TimeoutKey
}

[Serializable, NetSerializable]
public enum EjectWireKey : byte
{
    StatusKey
}
