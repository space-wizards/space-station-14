using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Power;

[Serializable, NetSerializable]
public sealed class ApcToolFinishedEvent : SimpleDoAfterEvent
{
}