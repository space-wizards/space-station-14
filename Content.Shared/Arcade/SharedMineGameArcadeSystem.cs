using Content.Shared.Power.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Arcade;

/// <summary>
/// This handles ent.Comp state networking for the MineGameArcade
/// </summary>
public abstract class SharedMineGameArcadeSystem : EntitySystem
{
    [Dependency] private readonly SharedPowerReceiverSystem _receiver = default!;

    public override void Initialize()
    {
        base.Initialize();

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
    /// <param name="ent">The entity hosting the arcade game.</param>
    /// <param name="boardSettings">The settings (board width/height/mine count) for the new board.</param>
    /// <returns>An initialized mine game state.</returns>
    public void SetupMineGame(Entity<MineGameArcadeComponent> ent, MineGameBoardSettings boardSettings)
    {
        ent.Comp.GameStatus = MineGameStatus.Initialized;
        // Validate what are potentially user-inputted numbers
        var clampedBoardSize = Vector2i.ComponentMin(Vector2i.ComponentMax(boardSettings.BoardSize, ent.Comp.MinBoardSize),
            ent.Comp.MaxBoardSize);

        // Replace with corrected values
        boardSettings.BoardSize = clampedBoardSize;
        boardSettings.MineCount = Math.Clamp(boardSettings.MineCount,
            ent.Comp.MinMineCount,
            clampedBoardSize.X * clampedBoardSize.Y);

        ent.Comp.ClearedCount = 0;
        ent.Comp.BoardSettings = boardSettings;
        ent.Comp.ReferenceTime = TimeSpan.Zero;
        ent.Comp.TileVisState = new MineGameTileVisState[boardSettings.BoardSize.X][];
        ent.Comp.TileMined = new bool[boardSettings.BoardSize.X][];
        for (int x = 0; x < boardSettings.BoardSize.X; ++x)
        {
            ent.Comp.TileVisState[x] = new MineGameTileVisState[boardSettings.BoardSize.Y];
            ent.Comp.TileMined[x] = new bool[boardSettings.BoardSize.Y];
        }
        Dirty(ent);
        UpdateUI(ent.Owner);
    }

    /// <summary>
    /// Handler for MineGameRequestNewBoardMessage, should create/set up a new board according to message parameters.
    /// </summary>
    public void OnMineGameRequestNewBoardMessage(Entity<MineGameArcadeComponent> ent, ref MineGameRequestNewBoardMessage msg)
    {
        if (!_receiver.IsPowered(ent.Owner) || Deleted(ent))
            return;
        SetupMineGame(ent, msg.Settings);
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
    /// <param name="ent">The entity hosting the arcade game.</param>
    /// <param name="actionLoc">The tile location to attempt to clear at/around.</param>
    public virtual void ClearTile(Entity<MineGameArcadeComponent> ent, Vector2i actionLoc) { }

    /// <summary>
    /// Fully performs a game tile action somewhere on the board, including clearing, flag/unflag, chording.
    /// </summary>
    /// <param name="ent">The entity hosting the arcade game.</param>
    /// <param name="msg">The message containing the action and tile the user picked.</param>
    public void OnMineGameTileAction(Entity<MineGameArcadeComponent> ent, ref MineGameTileActionMessage msg)
    {
        if (!_receiver.IsPowered(ent.Owner) || Deleted(ent))
            return;

        // Player shouldn't be able to do anything if the game isn't in a running state.
        if (ent.Comp.GameStatus != MineGameStatus.Initialized && ent.Comp.GameStatus != MineGameStatus.MinesSpawned)
            return;

        var actionLoc = Vector2i.ComponentMin(Vector2i.ComponentMax(msg.TileAction.TileLocation, Vector2i.Zero), ent.Comp.BoardSettings.BoardSize);
        switch (msg.TileAction.ActionType)
        {
            // Flagging is a simple operation which can be easily predicted and needs no knowledge of mines
            // Clearing, however, needs different client/server impls
            case MineGameTileActionType.Clear:
                ClearTile(ent, actionLoc);
                break;
            case MineGameTileActionType.Flag:
                if (ent.Comp.TileVisState[actionLoc.X][actionLoc.Y] == MineGameTileVisState.Uncleared)
                    ent.Comp.TileVisState[actionLoc.X][actionLoc.Y] = MineGameTileVisState.Flagged;
                break;
            case MineGameTileActionType.Unflag:
                if (ent.Comp.TileVisState[actionLoc.X][actionLoc.Y] == MineGameTileVisState.Flagged)
                    ent.Comp.TileVisState[actionLoc.X][actionLoc.Y] = MineGameTileVisState.Uncleared;
                break;
        }
        Dirty(ent);
        UpdateUI(ent.Owner);
    }
}
