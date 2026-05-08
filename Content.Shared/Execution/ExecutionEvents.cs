using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Execution;

[Serializable, NetSerializable]
public sealed partial class ExecutionDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
/// Raised on the victim when the doafter for an execution begins.
/// </summary>
[ByRefEvent]
public record struct ExecutionStartedEvent(EntityUid Attacker, EntityUid Weapon, bool CancelExecution = false, LocId? CancelMessage = null)
{
}
