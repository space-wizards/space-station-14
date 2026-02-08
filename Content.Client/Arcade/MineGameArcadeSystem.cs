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
        SubscribeLocalEvent<MineGameArcadeComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    /// <inheritdoc/>
    protected override void UpdateUI(EntityUid uid)
    {
        if (_ui.TryGetOpenUi(uid, MineGameArcadeUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }

    /// <inheritdoc/>
    public override void ClearTile(Entity<MineGameArcadeComponent> ent, Vector2i actionLoc)
    {
        // should only be zero when a game hasn't even started, and definitely not ended
        if (ent.Comp.ReferenceTime == TimeSpan.Zero)
            ent.Comp.ReferenceTime = _gameTiming.CurTime;
        if (ent.Comp.TileVisState[actionLoc.X][actionLoc.Y] == MineGameTileVisState.Uncleared)
            ent.Comp.TileVisState[actionLoc.X][actionLoc.Y] = MineGameTileVisState.ClearedEmpty;
    }

    private void OnHandleState(EntityUid uid, MineGameArcadeComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateUI(uid);
    }
}
