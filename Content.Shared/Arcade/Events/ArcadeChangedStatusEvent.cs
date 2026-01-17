namespace Content.Shared.Arcade.Events;

/// <summary>
///
/// </summary>
[ByRefEvent]
public record struct ArcadeChangedStateEvent(EntityUid? Player, ArcadeGameState OldState, ArcadeGameState NewState);
