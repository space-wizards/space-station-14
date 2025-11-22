using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadmanSwitch;

[Serializable, NetSerializable]
public sealed partial class DeadmanSwitchDoAfterEvent : SimpleDoAfterEvent
{
}