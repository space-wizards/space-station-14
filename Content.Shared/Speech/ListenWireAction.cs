using Robust.Shared.Serialization;

namespace Content.Shared.Speech;

[Serializable, NetSerializable]
public enum ListenWireActionKey : byte
{
    StatusKey,
    TimeoutKey,
}
