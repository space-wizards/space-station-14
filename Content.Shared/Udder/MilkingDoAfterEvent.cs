using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Udder;

[Serializable, NetSerializable]
public sealed partial class MilkingDoAfterEvent : SimpleDoAfterEvent
{
}
