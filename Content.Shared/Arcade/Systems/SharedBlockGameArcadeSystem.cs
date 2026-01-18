using Content.Shared.Arcade.Components;
using Content.Shared.Input;
using Content.Shared.Random.Helpers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Arcade.Systems;

/// <summary>
///
/// </summary>
public sealed partial class SharedBlockGameArcadeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedArcadeSystem _arcade = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockGameArcadeComponent, MapInitEvent>(OnMapInit);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ArcadeLeft, InputCmdHandler.FromDelegate(OnMoveLeft))
            .Bind(ContentKeyFunctions.ArcadeRight, InputCmdHandler.FromDelegate(OnMoveRight))
            .Register<SharedBlockGameArcadeSystem>();
    }

    private void OnMapInit(Entity<BlockGameArcadeComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Cells = new BlockGameArcadeCell[ent.Comp.Size.X * ent.Comp.Size.Y];
        Array.Fill(ent.Comp.Cells, BlockGameArcadeCell.Empty);
    }

    private void OnMoveLeft(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { Valid: true } player)
            return;

        if (!_ui.IsUiOpen(player, ArcadeUiKey.Key))
            return;

        if (!TryComp<ArcadePlayerComponent>(player, out var comp))
            return;

        if (comp.Arcade is not { Valid: true } arcade)
            return;

        if (!TryComp<BlockGameArcadeComponent>(arcade, out var blockGame))
            return;

        if (blockGame.MoveDirection != BlockGameMoveDirection.None)
            return;

        if (_arcade.GetGameState(arcade) != ArcadeGameState.Game)
            return;

        blockGame.MoveDirection = BlockGameMoveDirection.Left;
        DirtyField(arcade, blockGame, nameof(BlockGameArcadeComponent.MoveDirection));
    }

    private void OnMoveRight(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { Valid: true } player)
            return;

        if (!_ui.IsUiOpen(player, ArcadeUiKey.Key))
            return;

        if (!TryComp<ArcadePlayerComponent>(player, out var comp))
            return;

        if (comp.Arcade is not { Valid: true } arcade)
            return;

        if (!TryComp<BlockGameArcadeComponent>(arcade, out var blockGame))
            return;

        if (blockGame.MoveDirection != BlockGameMoveDirection.None)
            return;

        if (_arcade.GetGameState(arcade) != ArcadeGameState.Game)
            return;

        blockGame.MoveDirection = BlockGameMoveDirection.Right;
        DirtyField(arcade, blockGame, nameof(BlockGameArcadeComponent.MoveDirection));
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<BlockGameArcadeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_arcade.GetGameState(uid) != ArcadeGameState.Game)
                continue;

            if (comp.NextUpdate > curTime)
                continue;

            comp.NextUpdate += comp.UpdateCooldown;
            DirtyField(uid, comp, nameof(BlockGameArcadeComponent.NextUpdate));

            var width = comp.Size.X;
            for (var i = comp.Cells.Length - 1; i >= 0; i--)
            {
                var cell = comp.Cells[i];
                if (cell == BlockGameArcadeCell.Empty || cell == BlockGameArcadeCell.Player)
                    continue;

                var x = i / width;
                var newY = i % width - 1;
                if (newY > 0 && x == comp.PlayerPosition.X && newY == comp.PlayerPosition.Y)
                    _arcade.TryChangeGameState(uid, EntityUid.Invalid, ArcadeGameState.Lose);

                comp.Cells[i] = BlockGameArcadeCell.Empty;
            }

            if (comp.NextSpawn <= curTime)
            {
                // TODO: Use RandomPredicted https://github.com/space-wizards/RobustToolbox/pull/5849
                var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(uid).Id);
                var rand = new System.Random(seed);
                var pos = width * (uint)rand.NextInt64(0, width);
                if (comp.Cells[pos] != BlockGameArcadeCell.Empty)
                    comp.Cells[pos] = (BlockGameArcadeCell)(Enum.GetValues<BlockGameArcadeCell>().GetValue(rand.Next((int)BlockGameArcadeCell.Bonus, (int)BlockGameArcadeCell.Brown)) ?? BlockGameArcadeCell.Empty);

                comp.NextSpawn += comp.SpawnCooldown;
                DirtyField(uid, comp, nameof(BlockGameArcadeComponent.NextSpawn));
            }

            if (comp.MoveDirection != BlockGameMoveDirection.None)
            {
                var pos = width * comp.PlayerPosition.X;
                if (comp.Cells[pos] == BlockGameArcadeCell.Player)
                {
                    comp.Cells[pos] = BlockGameArcadeCell.Empty;
                    comp.Cells[width * comp.PlayerPosition.X + (comp.MoveDirection == BlockGameMoveDirection.Left ? -1 : 1)] = BlockGameArcadeCell.Player;
                }

                comp.MoveDirection = BlockGameMoveDirection.None;
                DirtyField(uid, comp, nameof(BlockGameArcadeComponent.MoveDirection));
            }

            DirtyField(uid, comp, nameof(BlockGameArcadeComponent.Cells));
        }
    }
}
