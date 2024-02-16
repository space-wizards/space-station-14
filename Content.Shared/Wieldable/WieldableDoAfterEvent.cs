using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Wieldable;

[Serializable, NetSerializable]
public sealed partial class WieldableDoAfterEvent : SimpleDoAfterEvent
{
}
