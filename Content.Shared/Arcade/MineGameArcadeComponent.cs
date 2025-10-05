using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Arcade;

[RegisterComponent, NetworkedComponent]
public sealed partial class MineGameArcadeComponent : Component
{
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
    /// Minimum allowed mine count for games on this component
    /// </summary>
    [DataField("boardPresets")]
    public Dictionary<string, MineGameBoardSettings> BoardPresets;

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

    /// <summary>
    /// Player-configurable board settings for this game specifically (XY dimensions, mine count)
    /// </summary>
    [ViewVariables]
    public MineGameBoardSettings BoardSettings;

    /// <summary>
    /// Counter tracking total number of tiles cleared from the board (can be recomputed directly from _tileVisState)
    /// </summary>
    [ViewVariables]
    public int ClearedCount;

    /// <summary>
    /// Whether a game has been initialized yet
    /// </summary>
    [ViewVariables]
    public bool GameInitialized;

    /// <summary>
    /// Whether a game's mines have been initialized yet
    /// </summary>
    /// <remarks>
    /// It is necessary to generate mines only after the initial clear because of the option for a safe-start
    /// tile radius, preventing mines from spawning on the first clear where the player has zero mine information.
    /// </remarks>
    [ViewVariables]
    public bool MinesGenerated;

    /// <summary>
    /// Time referencing the first clear (when the game is officially started)
    /// Null if game hasn't started
    /// </summary>
    [ViewVariables]
    public TimeSpan ReferenceTime;

    /// <summary>
    /// Whether the game was successfully won (and still in winning state; no new game)
    /// </summary>
    [ViewVariables]
    public bool GameWon;
    /// <summary>
    /// Whether the game was lost by hitting a mine (and still in loss state; no new game)
    /// </summary>
    [ViewVariables]
    public bool GameLost;

    /// <summary>
    /// Lookup for the proper visual state of any given tile.
    /// </summary>
    public MineGameTileVisState[,] TileVisState;

    /// <summary>
    /// Lookup for the tick a tile was last updated. Used for manually sending delta Tile state updates.
    /// </summary>
    public GameTick[,] TileLastVisUpdateTick;

    /// <summary>
    /// Lookup for whether tile at any given coordinates has a mine.
    /// </summary>
    /// <remarks>
    /// Should not be networked otherwise a client could guarantee a win. See also: MinesGenerated
    /// </remarks>
    public bool[,] TileMined;
}

/// <summary>
/// See MineGameArcadeComponent fields. TileVisState uniquely manually networked to note which tiles within the array
/// were actually dirtied/updated.
/// </summary>
[Serializable, NetSerializable]
public sealed class MineGameArcadeComponentState : ComponentState
{
    /// <inheritdoc cref="MineGameArcadeComponent.BoardSettings"/>
    public MineGameBoardSettings BoardSettings;
    /// <inheritdoc cref="MineGameArcadeComponent.ReferenceTime"/>
    public TimeSpan ReferenceTime;
    /// <inheritdoc cref="MineGameArcadeComponent.GameWon"/>
    public bool GameWon;
    /// <inheritdoc cref="MineGameArcadeComponent.GameLost"/>
    public bool GameLost;
    /// <summary>
    /// Manually networked and doesn't send the exact state as with the other fields; it is necessary to notify which
    /// specific indices/tile locations have been updated: expect entries with value MineGameTileVisState.None
    /// if no change was made to a specific location.
    /// </summary>
    /// <remarks>
    /// 1D array or other structure needed because multi-dim arrays aren't supported by NetSerializer
    /// </remarks>
    /// <inheritdoc cref="MineGameArcadeComponent.TileVisState"/>
    public required MineGameTileVisState[] TileVisState;
}
