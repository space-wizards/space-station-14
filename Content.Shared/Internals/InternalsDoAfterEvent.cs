using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Internals;

[Serializable, NetSerializable]
public sealed class InternalsDoAfterEvent : SimpleDoAfterEvent
{
}