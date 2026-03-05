using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Temperature;

/// <summary>
///     Fired on the target entity to check if it can be heated by a heating tool.
/// </summary>
[ByRefEvent]
public record struct HeatableAttemptEvent(EntityUid User, bool Cancelled = false);

/// <summary>
///     Event raised when a welder heating do-after completes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class WelderHeatDoAfterEvent : SimpleDoAfterEvent { }
