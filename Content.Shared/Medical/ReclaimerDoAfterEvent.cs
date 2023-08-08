using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical;

[Serializable, NetSerializable]
public sealed class ReclaimerDoAfterEvent : SimpleDoAfterEvent
{
}