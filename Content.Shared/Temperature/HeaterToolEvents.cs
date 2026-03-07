using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Temperature;

/// <summary>
///     Fired on the target entity to check if it can be heated by a heating tool.
/// </summary>
[ByRefEvent]
public record struct HeatableAttemptEvent(EntityUid User, bool Cancelled = false);

/// <summary>
///     Fired on the heater entity to check if it can heat.
/// </summary>
[ByRefEvent]
public record struct HeaterAttemptEvent(EntityUid User, bool Cancelled = false);

/// <summary>
///     Fired on the heater entity when it has successfully heated a solution.
/// </summary>
[ByRefEvent]
public record struct HeaterConsumedEvent(EntityUid User);

/// <summary>
///     Event raised when a heating tool do-after completes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class HeaterToolDoAfterEvent : SimpleDoAfterEvent { }
