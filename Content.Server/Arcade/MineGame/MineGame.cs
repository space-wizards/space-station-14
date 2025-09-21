using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using static Content.Shared.Arcade.MineGameShared;

namespace Content.Server.Arcade.MineGame;

/// <summary>
/// Handles game logic of the Mine Game.
/// </summary>
public sealed partial class MineGame
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    private readonly UserInterfaceSystem _uiSystem = default!;

    [ViewVariables]
    private Vector2i _boardSize;

    [ViewVariables]
    private int _mineCount;

    [ViewVariables]
    private int _flagCount;

    [ViewVariables]
    private int _clearedCount;

    [ViewVariables]
    private int _safeStartRadius;

    [ViewVariables]
    private bool _minesGenerated = false;

    [ViewVariables]
    private TimeSpan _referenceTime;

    private MineGameTileVisState[,] _tileVisState;
    private bool[,] _tileMined;

    [ViewVariables]
    public bool GameWon = false;
    [ViewVariables]
    public bool GameLost = false;


    public MineGame(Vector2i boardSize, int mineCount, int safeStartRadius)
    {
        IoCManager.InjectDependencies(this);
        _uiSystem = _entityManager.System<UserInterfaceSystem>();

        _boardSize = boardSize;
        _mineCount = mineCount;
        _safeStartRadius = safeStartRadius;
        _tileVisState = new MineGameTileVisState[_boardSize.X, _boardSize.Y];
        _tileMined = new bool[_boardSize.X, _boardSize.Y];
    }

    /// <summary>
    /// Places mines, filling in the _tileMined lookup.
    /// </summary>
    private void GenerateMines(Vector2i firstClearPos)
    {
        int safeSquareLength = Math.Max(_safeStartRadius * 2 - 1, 0);
        int safeTileCount = safeSquareLength * safeSquareLength;

        bool canGenSafeArea = _mineCount <= (_boardSize.X * _boardSize.Y - safeTileCount);
        // if the mine count is explicitly set higher than would normally be allowed by the
        // safe area... just let it happen w/out safe area; Don't play a 10x10 board with 100 mines.

        List<Vector2i> availablePositions = new();
        for (var y = 0; y < _boardSize.Y; ++y)
        {
            for (var x = 0; x < _boardSize.X; ++x)
            {
                var pos = new Vector2i(x, y);
                if (canGenSafeArea)
                {
                    var offsetToFirstClear = pos - firstClearPos;
                    offsetToFirstClear = new Vector2i(Math.Abs(offsetToFirstClear.X), Math.Abs(offsetToFirstClear.Y));
                    if (Math.Min(offsetToFirstClear.X, offsetToFirstClear.Y) < _safeStartRadius)
                        // Within safe starting area: don't allow this tile to be a mine
                        continue;
                }
                availablePositions.Add(pos);
            }
        }
        var minePositions = _random.GetItems(availablePositions, _mineCount, allowDuplicates: false);

        foreach (Vector2i minePos in minePositions)
        {
            _tileMined[minePos.X, minePos.Y] = true;
        }
        _minesGenerated = true;
    }

    /// <summary>
    /// Gets adjacent tile positions in 3x3 square, with border bounds check.
    /// </summary>
    /// <param name="pos">The center position as a vector.</param>
    /// <returns>List of neighboring tiles vector positions, also including the center: up to 9.</returns>
    private List<Vector2i> GetNeighborsOfPos(Vector2i pos)
    {
        List<Vector2i> neighborPositions = new();
        int ySt = Math.Max(pos.Y - 1, 0), yEn = Math.Min(pos.Y + 1, _boardSize.Y - 1);
        int xSt = Math.Max(pos.X - 1, 0), xEn = Math.Min(pos.X + 1, _boardSize.X - 1);
        for (var y = ySt; y <= yEn; ++y)
            for (var x = xSt; x <= xEn; ++x)
                neighborPositions.Add(new Vector2i(x, y));
        return neighborPositions;
    }

    /// <summary>
    /// Counts neighboring mines in 3x3 square
    /// </summary>
    /// <param name="pos">The center position as a vector.</param>
    /// <returns>Number of mines found</returns>
    private int CountMinesAroundPos(Vector2i pos)
    {
        int cnt = 0;
        foreach (Vector2i neighborPos in GetNeighborsOfPos(pos))
            if (_tileMined[neighborPos.X, neighborPos.Y])
                ++cnt;
        return cnt;
    }

    /// <summary>
    /// Counts neighboring flags in 3x3 square
    /// </summary>
    /// <param name="pos">The center position as a vector.</param>
    /// <returns>Number of flags found</returns>
    private int CountFlagsAroundPos(Vector2i pos)
    {
        int cnt = 0;
        foreach (Vector2i neighborPos in GetNeighborsOfPos(pos))
            if (_tileVisState[neighborPos.X, neighborPos.Y] == MineGameTileVisState.Flagged)
                ++cnt;
        return cnt;
    }

    /// <summary>
    /// Checks for win condition, and processes win and ends game if met.
    /// </summary>
    private void WinCheck(ref MineGameTileVisState[,] changedTileVisStates)
    {
        if (_clearedCount != _boardSize.X * _boardSize.Y - _mineCount)
            return;
        // Finish flagging all the mines
        for (var y = 0; y < _boardSize.Y; ++y)
            for (var x = 0; x < _boardSize.X; ++x)
                if (_tileMined[x, y])
                {
                    _tileVisState[x, y] = MineGameTileVisState.Flagged;
                    changedTileVisStates[x, y] = _tileVisState[x, y];
                }

        _referenceTime = _gameTiming.CurTime.Subtract(_referenceTime); // Save the completion duration
        GameWon = true;
    }

    /// <summary>
    /// Fully performs a game tile action somewhere on the board, including clearing, flag/unflag, chording.
    /// </summary>
    /// <param name="uid">The uid of the entity hosting the arcade game.</param>
    /// <param name="action">The action the user picked.</param>
    public void ExecutePlayerAction(EntityUid uid, MineGameTileAction action)
    {
        // Player shouldn't be able to do anything if the game isn't running.
        if (GameWon || GameLost)
            return;

        // Used for sending 'deltas' to clients; expressly for the purpose of allowing clients to remember local
        // 'predicted' states in some circumstances. Could be done better.
        MineGameTileVisState[,] changedTileVisStates = new MineGameTileVisState[_boardSize.X, _boardSize.Y];
        for (int y = 0; y < _boardSize.Y; ++y)
            for (int x = 0; x < _boardSize.X; ++x)
                changedTileVisStates[x, y] = MineGameTileVisState.None;

        switch (action.ActionType)
        {
            case MineGameTileActionType.Clear:
                if (!_minesGenerated)
                {
                    _referenceTime = _gameTiming.CurTime;
                    GenerateMines(action.TileLocation);
                }
                Queue<Vector2i> toClear = new();

                if (_tileVisState[action.TileLocation.X, action.TileLocation.Y] >= MineGameTileVisState.ClearedEmpty)
                {
                    // Chording
                    if (CountFlagsAroundPos(action.TileLocation) != CountMinesAroundPos(action.TileLocation))
                    {
                        // Correct number of flags is not around this number tile. Can't do anything.
                        return;
                    }
                    foreach (Vector2i neighborPos in GetNeighborsOfPos(action.TileLocation))
                    {
                        toClear.Enqueue(neighborPos);
                    }
                }
                else
                {
                    // Regular, single initial tile clear
                    toClear.Enqueue(action.TileLocation);
                }

                // Perform initial check to see if we hit a mine, before clearing anything (for proper chording display)
                foreach (var currPos in toClear)
                {
                    if (_tileMined[currPos.X, currPos.Y]
                        && _tileVisState[currPos.X, currPos.Y] != MineGameTileVisState.Flagged)
                    {
                        GameLost = true;
                        _tileVisState[currPos.X, currPos.Y] = MineGameTileVisState.MineDetonated;
                        changedTileVisStates[currPos.X, currPos.Y] = _tileVisState[currPos.X, currPos.Y];
                    }
                }
                if (GameLost)
                {
                    // We blew up at least one mine with this action!
                    _referenceTime = _gameTiming.CurTime.Subtract(_referenceTime);

                    // Show all remaining mines on the board
                    for (var y = 0; y < _boardSize.Y; ++y)
                    {
                        for (var x = 0; x < _boardSize.X; ++x)
                        {
                            if (_tileMined[x, y])
                            {
                                if (_tileVisState[x, y] != MineGameTileVisState.MineDetonated)
                                {
                                    _tileVisState[x, y] = MineGameTileVisState.Mine;
                                    changedTileVisStates[x, y] = _tileVisState[x, y];
                                }
                            }
                            else if (_tileVisState[x, y] == MineGameTileVisState.Flagged)
                            {
                                _tileVisState[x, y] = MineGameTileVisState.FalseFlagged;
                                changedTileVisStates[x, y] = _tileVisState[x, y];
                            }
                        }
                    }
                }
                else
                {
                    while (toClear.Count > 0)
                    {
                        var currPos = toClear.Dequeue();
                        if (_tileVisState[currPos.X, currPos.Y] != MineGameTileVisState.Uncleared)
                            continue;
                        int mineCount = CountMinesAroundPos(currPos);
                        _tileVisState[currPos.X, currPos.Y] = MineGameTileVisState.ClearedEmpty + mineCount;
                        changedTileVisStates[currPos.X, currPos.Y] = _tileVisState[currPos.X, currPos.Y];
                        ++_clearedCount;

                        if (mineCount == 0)
                        {
                            // Floodfill clear empty areas of the board
                            foreach (Vector2i neighborPos in GetNeighborsOfPos(currPos))
                            {
                                if (_tileVisState[neighborPos.X, neighborPos.Y] == MineGameTileVisState.Uncleared)
                                    toClear.Enqueue(neighborPos);
                            }
                        }
                    }
                }
                WinCheck(ref changedTileVisStates);
                UpdateUi(uid, changedTileVisStates);
                break;
            case MineGameTileActionType.Flag:
                if (_tileVisState[action.TileLocation.X, action.TileLocation.Y] == MineGameTileVisState.Uncleared)
                {
                    _tileVisState[action.TileLocation.X, action.TileLocation.Y] = MineGameTileVisState.Flagged;
                    changedTileVisStates[action.TileLocation.X, action.TileLocation.Y] = MineGameTileVisState.Flagged;
                    _flagCount++;
                }
                UpdateUi(uid, changedTileVisStates);
                break;
            case MineGameTileActionType.Unflag:
                if (_tileVisState[action.TileLocation.X, action.TileLocation.Y] == MineGameTileVisState.Flagged)
                {
                    _tileVisState[action.TileLocation.X, action.TileLocation.Y] = MineGameTileVisState.Uncleared;
                    changedTileVisStates[action.TileLocation.X, action.TileLocation.Y] = MineGameTileVisState.Uncleared;
                    _flagCount--;
                }
                UpdateUi(uid, changedTileVisStates);
                break;
        }
    }
}
