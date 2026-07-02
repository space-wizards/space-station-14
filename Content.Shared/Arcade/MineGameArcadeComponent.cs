using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Arcade;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class MineGameArcadeComponent : Component
{
    /// <summary>
    /// Minimum allowed board size for games on this component
    /// </summary>
    [DataField]
    public Vector2i MinBoardSize = new Vector2i(9, 9);

    /// <summary>
    /// Maximum allowed board size for games on this component
    /// </summary>
    [DataField]
    public Vector2i MaxBoardSize = new Vector2i(100, 100);

    /// <summary>
    /// Minimum allowed mine count for games on this component
    /// </summary>
    [DataField]
    public int MinMineCount = 1;

    /// <summary>
    /// Minimum allowed mine count for games on this component
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, MineGameBoardSettings> BoardPresets;

    /// <summary>
    /// "Radius" of safe starting area for the mine game. Safe square of side length SafeStartRadius*2-1.
    /// (Set <= 0 for no safe starting area)
    /// </summary>
    [DataField]
    public int SafeStartRadius = 2;

    /// <summary>
    /// Sound to be played when the player loses the game.
    /// </summary>
    [DataField]
    public SoundSpecifier? GameOverSound;

    /// <summary>
    /// Player-configurable board settings for this game specifically (XY dimensions, mine count)
    /// </summary>
    [AutoNetworkedField, ViewVariables]
    public MineGameBoardSettings BoardSettings;

    /// <summary>
    /// Counter tracking total number of tiles cleared from the board (can be recomputed directly from _tileVisState)
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public int ClearedCount;

    /// <inheritdoc cref="MineGameStatus"/>
    [AutoNetworkedField, ViewVariables]
    public MineGameStatus GameStatus;

    /// <summary>
    /// Time referencing either the first clear (when the game is officially started), or the final completion time
    /// Should be interpreted based on <see cref="GameStatus"/>
    /// </summary>
    [AutoNetworkedField, ViewVariables]
    public TimeSpan ReferenceTime;

    /// <summary>
    /// Lookup for the proper visual state of any given tile.
    /// </summary>
    /// <remarks>
    /// Multidimensional arrays aren't supported by NetSerializer so strange Jagged array is used instead;
    /// note that first index should still be X, ie each array within is a column of tiles
    /// </remarks>
    [AutoNetworkedField, ViewVariables]
    public MineGameTileVisState[][] TileVisState;

    /// <summary>
    /// Lookup for whether tile at any given coordinates has a mine.
    /// </summary>
    /// <remarks>
    /// Probably should not be networked otherwise a client could guarantee a win. See also: <see cref="GameStatus"/>
    /// </remarks>
    [ViewVariables]
    public bool[][] TileMined;
}
