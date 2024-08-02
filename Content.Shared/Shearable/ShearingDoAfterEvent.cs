using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Shearing;

[Serializable, NetSerializable]
public sealed partial class ShearingDoAfterEvent : SimpleDoAfterEvent
{
}
