using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.InfectorDead;

[Serializable, NetSerializable]
public sealed partial class InfectorDeadDoAfterEvent : SimpleDoAfterEvent
{
}
