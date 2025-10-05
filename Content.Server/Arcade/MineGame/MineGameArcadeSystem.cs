using System.Linq;
using Content.Shared.Arcade;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Arcade.MineGame;

/// <summary>
/// This handles the primary game logic for the MineGameArcade
/// </summary>
public sealed class MineGameArcadeSystem : SharedMineGameArcadeSystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MineGameArcadeComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MineGameArcadeComponent, PowerChangedEvent>(OnPowerChanged);
    }

    /// <inheritdoc/>
    public override void SetupMineGame(EntityUid uid,
        MineGameArcadeComponent component,
        MineGameBoardSettings boardSettings)
    {
        base.SetupMineGame(uid, component, boardSettings);
        boardSettings = component.BoardSettings; // base performed validation

        component.TileLastVisUpdateTick = new GameTick[boardSettings.BoardSize.X, boardSettings.BoardSize.Y];
        for (var y = 0; y < boardSettings.BoardSize.Y; ++y)
        {
            for (var x = 0; x < boardSettings.BoardSize.X; ++x)
            {
                component.TileLastVisUpdateTick[x, y] = _gameTiming.CurTick;
            }
        }
        Dirty(uid, component);
    }

    /// <inheritdoc/>
    public override void ClearTile(EntityUid uid, MineGameArcadeComponent component, Vector2i actionLoc)
    {
        if (!component.MinesGenerated)
        {
            component.ReferenceTime = _gameTiming.CurTime;
            GenerateMines(component, actionLoc);
        }
        Queue<Vector2i> toClear = new();

        if (component.TileVisState[actionLoc.X, actionLoc.Y] >= MineGameTileVisState.ClearedEmpty)
        {
            // Chording
            if (CountFlagsAroundPos(component, actionLoc) != CountMinesAroundPos(component, actionLoc))
            {
                // Correct number of flags is not around this number tile. Can't do anything.
                return;
            }
            foreach (Vector2i neighborPos in GetNeighborsOfPos(component, actionLoc))
            {
                toClear.Enqueue(neighborPos);
            }
        }
        else
        {
            // Regular, single initial tile clear
            toClear.Enqueue(actionLoc);
        }

        var mineHit = false;
        // Perform initial check to see if we hit a mine, before clearing anything (for proper chording display)
        foreach (var currPos in toClear)
        {
            if (component.TileMined[currPos.X, currPos.Y]
                && component.TileVisState[currPos.X, currPos.Y] != MineGameTileVisState.Flagged)
            {
                mineHit = true;
                SetTileState(component, currPos.X, currPos.Y, MineGameTileVisState.MineDetonated);
            }
        }
        if (mineHit)
        {
            // We blew up at least one mine with this action! Game over.
            component.GameLost = true;
            component.ReferenceTime = _gameTiming.CurTime.Subtract(component.ReferenceTime);

            // Show all remaining mines on the board
            for (var y = 0; y < component.BoardSettings.BoardSize.Y; ++y)
            {
                for (var x = 0; x < component.BoardSettings.BoardSize.X; ++x)
                {
                    if (component.TileMined[x, y])
                    {
                        if (component.TileVisState[x, y] != MineGameTileVisState.MineDetonated)
                        {
                            SetTileState(component, x, y, MineGameTileVisState.Mine);
                        }
                    }
                    else if (component.TileVisState[x, y] == MineGameTileVisState.Flagged)
                    {
                        SetTileState(component, x, y, MineGameTileVisState.FalseFlagged);
                    }
                }
            }
            _audioSystem.PlayPvs(component.GameOverSound, uid, AudioParams.Default.WithVolume(-5f));
        }
        else
        {
            while (toClear.Count > 0)
            {
                var currPos = toClear.Dequeue();
                if (component.TileVisState[currPos.X, currPos.Y] != MineGameTileVisState.Uncleared)
                    continue;
                int mineCount = CountMinesAroundPos(component, currPos);
                SetTileState(component, currPos.X, currPos.Y, MineGameTileVisState.ClearedEmpty + (byte)mineCount);
                ++component.ClearedCount;

                if (mineCount == 0)
                {
                    // Floodfill clear empty areas of the board
                    foreach (Vector2i neighborPos in GetNeighborsOfPos(component, currPos))
                    {
                        if (component.TileVisState[neighborPos.X, neighborPos.Y] == MineGameTileVisState.Uncleared)
                            toClear.Enqueue(neighborPos);
                    }
                }
            }
        }
        WinCheck(component);
    }

    private void OnComponentInit(EntityUid uid, MineGameArcadeComponent component, ComponentInit args)
    {
        if (component.BoardPresets.Count > 0 && !component.GameInitialized)
            SetupMineGame(uid, component, component.BoardPresets.First().Value);
    }

    private void OnPowerChanged(EntityUid uid, MineGameArcadeComponent component, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        _uiSystem.CloseUi(uid, MineGameArcadeUiKey.Key);
    }

    /// <summary>
    /// Places mines, filling in the component.TileMined lookup.
    /// </summary>
    private void GenerateMines(MineGameArcadeComponent component, Vector2i firstClearPos)
    {
        int safeSquareLength = Math.Max(component.SafeStartRadius * 2 - 1, 0);
        int safeTileCount = safeSquareLength * safeSquareLength;

        bool canGenSafeArea = component.BoardSettings.MineCount <= (component.BoardSettings.BoardSize.X * component.BoardSettings.BoardSize.Y - safeTileCount);
        // if the mine count is explicitly set higher than would normally be allowed by the
        // safe area... just let it happen w/out safe area; Don't play a 10x10 board with 100 mines.

        List<Vector2i> availablePositions = new();
        for (var y = 0; y < component.BoardSettings.BoardSize.Y; ++y)
        {
            for (var x = 0; x < component.BoardSettings.BoardSize.X; ++x)
            {
                var pos = new Vector2i(x, y);
                if (canGenSafeArea)
                {
                    var offsetToFirstClear = pos - firstClearPos;
                    offsetToFirstClear = new Vector2i(Math.Abs(offsetToFirstClear.X), Math.Abs(offsetToFirstClear.Y));
                    if (Math.Min(offsetToFirstClear.X, offsetToFirstClear.Y) < component.SafeStartRadius)
                        // Within safe starting area: don't allow this tile to be a mine
                        continue;
                }
                availablePositions.Add(pos);
            }
        }
        var minePositions = _random.GetItems(availablePositions, component.BoardSettings.MineCount, allowDuplicates: false);

        foreach (Vector2i minePos in minePositions)
        {
            component.TileMined[minePos.X, minePos.Y] = true;
        }

        component.MinesGenerated = true;
    }

    /// <summary>
    /// Gets adjacent tile positions in 3x3 square, with border bounds check.
    /// </summary>
    /// <param name="pos">The center position as a vector.</param>
    /// <returns>List of neighboring tiles vector positions, also including the center: up to 9.</returns>
    private List<Vector2i> GetNeighborsOfPos(MineGameArcadeComponent component, Vector2i pos)
    {
        List<Vector2i> neighborPositions = new();
        int ySt = Math.Max(pos.Y - 1, 0), yEn = Math.Min(pos.Y + 1, component.BoardSettings.BoardSize.Y - 1);
        int xSt = Math.Max(pos.X - 1, 0), xEn = Math.Min(pos.X + 1, component.BoardSettings.BoardSize.X - 1);
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
    private int CountMinesAroundPos(MineGameArcadeComponent component, Vector2i pos)
    {
        int cnt = 0;
        foreach (Vector2i neighborPos in GetNeighborsOfPos(component, pos))
            if (component.TileMined[neighborPos.X, neighborPos.Y])
                ++cnt;
        return cnt;
    }

    /// <summary>
    /// Counts neighboring flags in 3x3 square
    /// </summary>
    /// <param name="pos">The center position as a vector.</param>
    /// <returns>Number of flags found</returns>
    private int CountFlagsAroundPos(MineGameArcadeComponent component, Vector2i pos)
    {
        int cnt = 0;
        foreach (Vector2i neighborPos in GetNeighborsOfPos(component, pos))
            if (component.TileVisState[neighborPos.X, neighborPos.Y] == MineGameTileVisState.Flagged)
                ++cnt;
        return cnt;
    }

    /// <summary>
    /// Checks for win condition, and processes win and ends game if met.
    /// </summary>
    private void WinCheck(MineGameArcadeComponent component)
    {
        if (component.ClearedCount != component.BoardSettings.BoardSize.X * component.BoardSettings.BoardSize.Y - component.BoardSettings.MineCount)
            return;
        // Finish flagging all the mines
        for (var y = 0; y < component.BoardSettings.BoardSize.Y; ++y)
        {
            for (var x = 0; x < component.BoardSettings.BoardSize.X; ++x)
            {
                if (component.TileMined[x, y])
                {
                    component.TileVisState[x, y] = MineGameTileVisState.Flagged;
                    component.TileLastVisUpdateTick[x, y] = _gameTiming.CurTick;
                }
            }
        }

        component.GameWon = true;
        component.ReferenceTime = _gameTiming.CurTime.Subtract(component.ReferenceTime); // Save the completion duration
    }

    /// <inheritdoc/>
    public override void SetTileState(MineGameArcadeComponent component, int x, int y, MineGameTileVisState state)
    {
        base.SetTileState(component, x, y, state);
        component.TileLastVisUpdateTick[x, y] = _gameTiming.CurTick;
    }
}
