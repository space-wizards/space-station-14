using Content.Server.Arcade.Components;
using Content.Server.Arcade.Prototypes;
using Content.Server.GameTicking.Events;
using Content.Shared.Arcade.Systems;
using Content.Shared.EntityTable;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Arcade.Systems;

public sealed partial class ArcadeSystem : SharedArcadeSystem
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArcadeRewardComponent, ComponentInit>(OnArcadeRewardComponentInit);
        SubscribeLocalEvent<ArcadeRewardComponent, ArcadeGameEndedEvent>(OnArcadeRewardGameEnded);
        SubscribeLocalEvent<ArcadeScoreboardComponent, ArcadeGameEndedEvent>(OnArcadeScoreboardGameEnded);

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        _prototypeManager.PrototypesReloaded += OnPrototypesReloaded;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ArcadeScoreboardPrototype>())
            FillMissingScoreboards();
    }

    /// <summary>
    ///     Ends the arcade game on a win.
    /// </summary>
    /// <param name="player">The entity playing the game.</param>
    /// <param name="machine">The arcade machine entity.</param>
    [PublicAPI]
    public void WinGame(EntityUid? player, EntityUid machine, int? score = null)
    {
        FinishGame(player, machine, ArcadeGameResult.Win, score);
    }

    /// <summary>
    ///     Ends the arcade game on a loss.
    /// </summary>
    /// <param name="player">The entity playing the game.</param>
    /// <param name="machine">The arcade machine entity.</param>
    [PublicAPI]
    public void LoseGame(EntityUid? player, EntityUid machine, int? score = null)
    {
        FinishGame(player, machine, ArcadeGameResult.Fail, score);
    }

    /// <summary>
    ///     Ends the arcade game without finishing it (i.e. quitting early).
    /// </summary>
    /// <param name="player">The entity playing the game.</param>
    /// <param name="machine">The arcade machine entity.</param>
    [PublicAPI]
    public void LeaveGame(EntityUid? player, EntityUid machine, int? score = null)
    {
        FinishGame(player, machine, ArcadeGameResult.Forfeit, score);
    }

    /// <summary>
    ///     Ends the arcade game on a draw (game finished, neither win nor lose).
    /// </summary>
    /// <param name="player">The entity playing the game.</param>
    /// <param name="machine">The arcade machine entity.</param>
    [PublicAPI]
    public void DrawGame(EntityUid? player, EntityUid machine, int? score = null)
    {
        FinishGame(player, machine, ArcadeGameResult.Draw, score);
    }

    private void FinishGame(EntityUid? player, EntityUid machine, ArcadeGameResult result, int? score = null)
    {
        var endedEvent = new ArcadeGameEndedEvent(player, result, score);
        var finishEvent = new FinishedArcadeGameEvent(result, score);

        RaiseLocalEvent(machine, endedEvent);
        if (player != null)
            RaiseLocalEvent(player.Value, finishEvent);
    }
}

/// <summary>
///     Called on the arcade machine entity when a game ends for any reason.
/// </summary>
/// <param name="player">The entity playing the arcade game.</param>
/// <param name="result">The result of the game.</param>
public sealed class ArcadeGameEndedEvent(EntityUid? player,
    ArcadeGameResult result = ArcadeGameResult.Forfeit,
    int? score = null)
    : EntityEventArgs
{
    public EntityUid? Player = player;
    public ArcadeGameResult Result = result;
    public int? Score = score;
}

/// <summary>
///     Called on the arcade game player entity when they finish an arcade game for any reason.
/// </summary>
/// <param name="result">The result of the game.</param>
public sealed class FinishedArcadeGameEvent(ArcadeGameResult result, int? score = null) : EntityEventArgs
{
    public ArcadeGameResult Result = result;
    public int? Score = score;
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
