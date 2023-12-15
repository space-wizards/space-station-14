using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Spillable;

[Serializable, NetSerializable]
public sealed partial class SpillDoAfterEvent : SimpleDoAfterEvent
{
}
