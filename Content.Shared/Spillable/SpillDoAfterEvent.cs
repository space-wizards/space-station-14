using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Spillable;

[Serializable, NetSerializable]
public sealed class SpillDoAfterEvent : SimpleDoAfterEvent
{
}