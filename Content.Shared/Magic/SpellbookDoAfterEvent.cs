using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Magic;

[Serializable, NetSerializable]
public sealed class SpellbookDoAfterEvent : SimpleDoAfterEvent
{
}