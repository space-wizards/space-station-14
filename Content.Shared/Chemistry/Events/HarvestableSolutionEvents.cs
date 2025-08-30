using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Events;

[Serializable, NetSerializable]
public sealed partial class HarvestableSolutionDoAfterEvent : SimpleDoAfterEvent
{
}
