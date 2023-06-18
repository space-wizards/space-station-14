using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Extinguisher.Events;

/// <summary>
///     Raised after welding do_after has finished. It doesn't guarantee success,
///     use <see cref="CoolableEvent"/> to get updated status.
/// </summary>
[Serializable, NetSerializable]
public sealed class CoolingFinishedDoAfterEvent : SimpleDoAfterEvent
{
}
