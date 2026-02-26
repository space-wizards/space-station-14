using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Guardian;

[NetworkedComponent, Serializable, NetSerializable]
public sealed partial class GuardianCreatorDoAfterEvent : SimpleDoAfterEvent
{
}
