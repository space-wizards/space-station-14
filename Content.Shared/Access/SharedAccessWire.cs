using Robust.Shared.Serialization;

namespace Content.Shared.Access;

[Serializable, NetSerializable]
public enum AccessWireActionKey : byte
{
    Key,
    Status,
    Pulsed,
    PulseCancel
}

[Serializable, NetSerializable]
public enum LogWireActionKey : byte
{
    Key,
    Status,
    Pulsed,
    PulseCancel
}
