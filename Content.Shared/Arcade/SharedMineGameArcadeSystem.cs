using Content.Shared.Power.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Arcade;

/// <summary>
/// This handles component state networking for the MineGameArcade
/// </summary>
public abstract class SharedMineGameArcadeSystem : EntitySystem
{
    [Dependency] private readonly SharedPowerReceiverSystem _receiver = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MineGameArcadeComponent, ComponentGetState>(OnGetState);

        Subs.BuiEvents<MineGameArcadeComponent>(MineGameArcadeUiKey.Key, subs =>
        {
            subs.Event<MineGameTileActionMessage>(OnMineGameTileAction);
            subs.Event<MineGameRequestNewBoardMessage>(OnMineGameRequestNewBoardMessage);
        });
    }

    /// <summary>
    /// Generates an array of tile visual states based on the current game board state, and packs width
    /// and other metadata like game time/status/mine count.
    /// </summary>
    /// <param name="uid">The uid of the entity hosting the arcade game.</param>
    /// <param name="component">The arcade component hosting the game.</param>
    /// <param name="boardSettings">The settings (board width/height/mine count) for the new board.</param>
    /// <returns>An initialized mine game state.</returns>
    public virtual void SetupMineGame(EntityUid uid, MineGameArcadeComponent component, MineGameBoardSettings boardSettings)
    {
        component.GameInitialized = true;
        // Validate what are potentially user-inputted numbers
        var clampedBoardSize = Vector2i.ComponentMin(Vector2i.ComponentMax(boardSettings.BoardSize, component.MinBoardSize),
            component.MaxBoardSize);

        // Replace with corrected values
        boardSettings.BoardSize = clampedBoardSize;
        boardSettings.MineCount = Math.Clamp(boardSettings.MineCount,
            component.MinMineCount,
            clampedBoardSize.X * clampedBoardSize.Y);

        component.GameLost = false;
        component.GameWon = false;
        component.ClearedCount = 0;
        component.MinesGenerated = false;
        component.BoardSettings = boardSettings;
        component.ReferenceTime = TimeSpan.Zero;
        component.TileVisState = new MineGameTileVisState[boardSettings.BoardSize.X, boardSettings.BoardSize.Y];
        component.TileMined = new bool[boardSettings.BoardSize.X, boardSettings.BoardSize.Y];
        UpdateUI(uid);
    }

    /// <summary>
    /// Handler for MineGameRequestNewBoardMessage, should create/set up a new board according to message parameters.
    /// </summary>
    public void OnMineGameRequestNewBoardMessage(EntityUid uid, MineGameArcadeComponent component, MineGameRequestNewBoardMessage msg)
    {
        if (!_receiver.IsPowered(uid) || Deleted(uid))
            return;
        SetupMineGame(uid, component, msg.Settings);
    }

    /// <summary>
    /// Attempts to update the BUI for the entity if it exists.
    /// </summary>
    protected virtual void UpdateUI(EntityUid uid) { }

    /// <summary>
    /// Performs a clearing operation at a specific tile location on a board. A clear initiating on a regular uncleared
    /// tile is expected to clear the single tile, while trying to clear any already cleared tile will attempt to
    /// perform chording and may clear multiple adjacent tiles. All cleared tiles floodfill clear empty space confirmed
    /// to be void of any adjacent mines.
    /// </summary>
    /// <param name="uid">The uid of the entity hosting the arcade game.</param>
    /// <param name="component">The arcade component hosting the game.</param>
    /// <param name="actionLoc">The tile location to attempt to clear at/around.</param>
    public virtual void ClearTile(EntityUid uid, MineGameArcadeComponent component, Vector2i actionLoc) { }

    /// <summary>
    /// Sets the visual state for a tile at a certain position.
    /// </summary>
    public virtual void SetTileState(MineGameArcadeComponent component, int x, int y, MineGameTileVisState state)
    {
        component.TileVisState[x, y] = state;
    }

    /// <summary>
    /// Fully performs a game tile action somewhere on the board, including clearing, flag/unflag, chording.
    /// </summary>
    /// <param name="uid">The uid of the entity hosting the arcade game.</param>
    /// <param name="component">The component holding the arcade game data.</param>
    /// <param name="msg">The message containing the action and tile the user picked.</param>
    public void OnMineGameTileAction(EntityUid uid, MineGameArcadeComponent component, MineGameTileActionMessage msg)
    {
        if (!_receiver.IsPowered(uid) || Deleted(uid))
            return;

        // Player shouldn't be able to do anything if the game has already ended.
        if (component.GameWon || component.GameLost)
            return;

        var actionLoc = Vector2i.ComponentMin(Vector2i.ComponentMax(msg.TileAction.TileLocation, Vector2i.Zero), component.BoardSettings.BoardSize);
        switch (msg.TileAction.ActionType)
        {
            // Flagging is a simple operation which can be easily predicted and needs no knowledge of mines
            // Clearing, however, needs different client/server impls
            case MineGameTileActionType.Clear:
                ClearTile(uid, component, actionLoc);
                break;
            case MineGameTileActionType.Flag:
                if (component.TileVisState[actionLoc.X, actionLoc.Y] == MineGameTileVisState.Uncleared)
                    SetTileState(component, actionLoc.X, actionLoc.Y, MineGameTileVisState.Flagged);
                break;
            case MineGameTileActionType.Unflag:
                if (component.TileVisState[actionLoc.X, actionLoc.Y] == MineGameTileVisState.Flagged)
                    SetTileState(component, actionLoc.X, actionLoc.Y, MineGameTileVisState.Uncleared);
                break;
        }
        Dirty(uid, component);
        UpdateUI(uid);
    }

    private void OnGetState(EntityUid uid, MineGameArcadeComponent component, ref ComponentGetState args)
    {
        var isFullState = args.FromTick <= component.CreationTick;

        var tileStates = new MineGameTileVisState[component.BoardSettings.BoardSize.X * component.BoardSettings.BoardSize.Y];
        for (var y = 0; y < component.BoardSettings.BoardSize.Y; ++y)
        {
            for (var x = 0; x < component.BoardSettings.BoardSize.X; ++x)
            {
                var tileState = MineGameTileVisState.None;
                if (isFullState || args.FromTick <= component.TileLastVisUpdateTick[x, y])
                {
                    tileState = component.TileVisState[x, y];
                }
                tileStates[y * component.BoardSettings.BoardSize.X + x] = tileState;
            }
        }

        args.State = new MineGameArcadeComponentState()
        {
            BoardSettings = component.BoardSettings,
            ReferenceTime = component.ReferenceTime,
            GameWon = component.GameWon,
            GameLost = component.GameLost,
            TileVisState = tileStates
        };
    }
}
