namespace Content.Shared.Arcade.Events;

/// <summary>
///
/// </summary>
/// <param name="Player"></param>
/// <param name="OldState"></param>
/// <param name="NewState"></param>
[ByRefEvent]
public record struct ArcadeChangedStateEvent(EntityUid? Player, ArcadeGameState OldState, ArcadeGameState NewState);
