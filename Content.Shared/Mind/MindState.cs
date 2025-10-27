using Robust.Shared.Serialization;

namespace Content.Shared.Mind;

[Serializable, NetSerializable]
public enum MindState : byte
{
    DeadNoUID,
    DeadHasUID,
    DeadHasSession,
    AliveNoUID,
    AliveNoSession,
    AliveHasSession
}
