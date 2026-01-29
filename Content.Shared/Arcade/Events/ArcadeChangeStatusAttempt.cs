using Content.Shared.Arcade.Enums;

namespace Content.Shared.Arcade.Events;

/// <summary>
///
/// </summary>
[ByRefEvent]
public record struct ArcadeChangeStateAttempt(EntityUid? Player, ArcadeGameState CurrentState, ArcadeGameState DesiredState, bool Cancelled = false);
