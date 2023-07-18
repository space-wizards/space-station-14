using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Gatherable;

[Serializable, NetSerializable]
public sealed class GatherableDoAfterEvent : SimpleDoAfterEvent
{
}