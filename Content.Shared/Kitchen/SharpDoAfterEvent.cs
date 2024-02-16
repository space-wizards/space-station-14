using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen;

[Serializable, NetSerializable]
public sealed partial class SharpDoAfterEvent : SimpleDoAfterEvent
{
}
