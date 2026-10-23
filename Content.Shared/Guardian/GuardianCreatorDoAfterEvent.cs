using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Guardian;

[Serializable, NetSerializable]
public sealed partial class GuardianCreatorDoAfterEvent : SimpleDoAfterEvent
{
}
