using Content.Shared.Arcade;

namespace Content.Server.Arcade;

public sealed partial class ArcadeSystem : SharedArcadeSystem
{
}

/// <summary>
///     Called on the arcade machine entity when a game ends for any reason.
/// </summary>
/// <param name="player">The entity playing the arcade game.</param>
/// <param name="result">The result of the game.</param>
public sealed class ArcadeGameEndedEvent(EntityUid? player,
    ArcadeGameResult result = ArcadeGameResult.Forfeit)
    : EntityEventArgs
{
    public EntityUid? Player = player;
    public ArcadeGameResult Result = result;
}

/// <summary>
///     Called on the arcade game player entity when they finish an arcade game for any reason.
/// </summary>
/// <param name="result">The result of the game.</param>
public sealed class FinishedArcadeGameEvent(ArcadeGameResult result) : EntityEventArgs
{
    public ArcadeGameResult Result = result;
}

public enum ArcadeGameResult
{
    /// <summary>
    /// Player has won the game.
    /// </summary>
    Win,

    /// <summary>
    /// Game ends, and the player neither won nor lost.
    /// </summary>
    Draw,

    /// <summary>
    /// The player forfeits the game.
    /// </summary>
    Forfeit,

    /// <summary>
    /// The player lost the game.
    /// </summary>
    Fail,
}
