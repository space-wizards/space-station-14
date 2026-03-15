using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Temperature;

/// <summary>
///     Fired on the heater and target entities to check if they can heat or be heated.
/// </summary>
[ByRefEvent]
public record struct HeaterAttemptEvent(EntityUid User, float FrequencyMultiplier = 1f, bool Cancelled = false);

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
