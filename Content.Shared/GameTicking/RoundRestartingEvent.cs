using Robust.Shared.Serialization;

namespace Content.Shared.GameTicking;

/// <summary>
/// Event notifying about the restart of the round
/// </summary>
[ByRefEvent]
public readonly record struct RoundRestartingEvent();
