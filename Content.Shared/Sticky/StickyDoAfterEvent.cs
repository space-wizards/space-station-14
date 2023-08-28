using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Sticky;

[Serializable, NetSerializable]
public sealed partial class StickyDoAfterEvent : SimpleDoAfterEvent
{
}
