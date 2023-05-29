using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Spaceshroom;

[Serializable, NetSerializable]
public sealed class GatherableByHandDoAfterEvent : SimpleDoAfterEvent
{
}
