using Robust.Shared.Serialization;

namespace Content.Shared.Access;

[Serializable, NetSerializable]
public enum AccessWireActionKey
{
    Key,
    Status,
    Pulsed,
    PulseCancel
}
