using JetBrains.Annotations;

namespace Content.Shared.Throwing;

/// <summary>
///     Raised on thrown entity.
/// </summary>
[PublicAPI]
[ByRefEvent]
public readonly record struct ThrownEvent(EntityUid? User, EntityUid Thrown);
