using Content.Shared.Arcade;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Client.Arcade;

public sealed class MineGameArcadeSystem : SharedMineGameArcadeSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MineGameArcadeComponent, ComponentHandleState>(OnHandleState);
    }

    protected override void UpdateUI(EntityUid uid)
    {
        if (_ui.TryGetOpenUi(uid, MineGameArcadeUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }

    /// <inheritdoc/>
    public override void ClearTile(EntityUid uid, MineGameArcadeComponent component, Vector2i actionLoc)
    {
        // should only be zero when a game hasn't even started, and definitely not ended
        if (component.ReferenceTime == TimeSpan.Zero)
            component.ReferenceTime = _gameTiming.CurTime;
        if (component.TileVisState[actionLoc.X, actionLoc.Y] == MineGameTileVisState.Uncleared)
            component.TileVisState[actionLoc.X, actionLoc.Y] = MineGameTileVisState.ClearedEmpty;
    }

    private void OnHandleState(EntityUid uid, MineGameArcadeComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MineGameArcadeComponentState state)
            return;

        bool sizeChanged = state.BoardSettings.BoardSize.X != component.BoardSettings.BoardSize.X || state.BoardSettings.BoardSize.Y != component.BoardSettings.BoardSize.Y;
        if (sizeChanged)
            component.TileVisState = new MineGameTileVisState[state.BoardSettings.BoardSize.X, state.BoardSettings.BoardSize.Y];
        for (var y = 0; y < state.BoardSettings.BoardSize.Y; ++y)
        {
            for (var x = 0; x < state.BoardSettings.BoardSize.X; ++x)
            {
                var tileState = state.TileVisState[y * state.BoardSettings.BoardSize.X + x];
                if (tileState != MineGameTileVisState.None)
                {
                    component.TileVisState[x, y] = tileState;
                }
            }
        }

        component.BoardSettings = state.BoardSettings;
        component.ReferenceTime = state.ReferenceTime;
        component.GameWon = state.GameWon;
        component.GameLost = state.GameLost;

        UpdateUI(uid);
    }
}
