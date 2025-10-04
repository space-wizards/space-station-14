using Robust.Shared.Audio;

namespace Content.Server.Arcade.MineGame;

[RegisterComponent]
public sealed partial class MineGameArcadeComponent : Component
{
    /// <summary>
    /// The currently active Mine Game session.
    /// </summary>
    [ViewVariables]
    public MineGame? Game = null;

    /// <summary>
    /// Minimum allowed board size for games on this component
    /// </summary>
    [DataField("minBoardSize")]
    public Vector2i MinBoardSize = new Vector2i(9, 9);

    /// <summary>
    /// Maximum allowed board size for games on this component
    /// </summary>
    [DataField("maxBoardSize")]
    public Vector2i MaxBoardSize = new Vector2i(100, 100);

    /// <summary>
    /// Minimum allowed mine count for games on this component
    /// </summary>
    [DataField("minMineCount")]
    public int MinMineCount = 1;

    /// <summary>
    /// "Radius" of safe starting area for the mine game. Safe square of side length SafeStartRadius*2-1.
    /// (Set <= 0 for no safe starting area)
    /// </summary>
    [DataField("safeStartRadius")]
    public int SafeStartRadius = 2;

    /// <summary>
    /// Sound to be played when the player loses the game.
    /// </summary>
    [DataField("gameOverSound")]
    public SoundSpecifier? GameOverSound;
}
