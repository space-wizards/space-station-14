namespace Content.Shared.Arcade.Events;

/// <summary>
///
/// </summary>
/// <param name="Player"></param>
/// <param name="CurrentState"></param>
/// <param name="DesiredState"></param>
/// <param name="Cancelled"></param>
[ByRefEvent]
public record struct ArcadeChangeStateAttempt(EntityUid? Player, ArcadeGameState CurrentState, ArcadeGameState DesiredState, bool Cancelled = false);
