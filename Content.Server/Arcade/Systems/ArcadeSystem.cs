using Content.Server.Arcade.Components;
using Content.Server.Arcade.Prototypes;
using Content.Shared.Arcade.Systems;
using Content.Shared.EntityTable;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.Arcade.Systems;

public sealed partial class ArcadeSystem : SharedArcadeSystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArcadeRewardComponent, ComponentInit>(OnArcadeRewardComponentInit);
        SubscribeLocalEvent<ArcadeRewardComponent, ArcadeGameEndedEvent>(OnArcadeRewardGameEnded);
        SubscribeLocalEvent<ArcadeScoreboardComponent, ArcadeGameEndedEvent>(OnArcadeScoreboardGameEnded);

        _prototypeManager.PrototypesReloaded += OnPrototypesReloaded;
        InitializeScoreboards();
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
    /// <param name="score">The (optional) final score associated with this game session.</param>
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
    /// <param name="score">The (optional) final score associated with this game session.</param>
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
    /// <param name="score">The (optional) final score associated with this game session.</param>
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
    /// <param name="score">The (optional) final score associated with this game session.</param>
    [PublicAPI]
    public void DrawGame(EntityUid? player, EntityUid machine, int? score = null)
    {
        FinishGame(player, machine, ArcadeGameResult.Draw, score);
    }

    private void FinishGame(EntityUid? player, EntityUid machine, ArcadeGameResult result, int? score = null)
    {
        var endedEvent = new ArcadeGameEndedEvent(player, result, score);
        var finishEvent = new FinishedArcadeGameEvent(result, score);

        RaiseLocalEvent(machine, ref endedEvent);
        if (player != null)
            RaiseLocalEvent(player.Value, ref finishEvent);
    }
}

/// <summary>
///     Called on the arcade machine entity when a game ends for any reason.
/// </summary>
/// <param name="Player">The entity playing the arcade game.</param>
/// <param name="Result">The result of the game.</param>
/// <param name="Score">The (optional) final score associated with this game session.</param>
[ByRefEvent]
public record struct ArcadeGameEndedEvent(EntityUid? Player,
    ArcadeGameResult Result = ArcadeGameResult.Forfeit,
    int? Score = null)
{
    public EntityUid? Player = Player;
    public ArcadeGameResult Result = Result;
    public int? Score = Score;
}

/// <summary>
///     Called on the arcade game player entity when they finish an arcade game for any reason.
/// </summary>
/// <param name="Result">The result of the game.</param>
/// <param name="Score">The (optional) final score associated with this game session.</param>
[ByRefEvent]
public record struct FinishedArcadeGameEvent(ArcadeGameResult Result, int? Score = null)
{
    public ArcadeGameResult Result = Result;
    public int? Score = Score;
}

/// <summary>
/// The outcome of a completed arcade game session.
/// </summary>
/// <remarks>
/// Entity systems may use this result to perform certain logic - for example, only dispensing a prize if the game is won.
/// </remarks>
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
