using System.Diagnostics.CodeAnalysis;
using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Enums;
using Content.Shared.Arcade.Events;
using Content.Shared.Input;
using Content.Shared.Random.Helpers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Random;
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

        SubscribeLocalEvent<BlockGameArcadeComponent, ArcadeChangedStateEvent>(OnArcadeChangedState);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ArcadeLeft, InputCmdHandler.FromDelegate(OnMoveLeft))
            .Bind(ContentKeyFunctions.ArcadeUp, InputCmdHandler.FromDelegate(OnMoveUp))
            .Bind(ContentKeyFunctions.ArcadeDown, InputCmdHandler.FromDelegate(OnMoveDown))
            .Bind(ContentKeyFunctions.ArcadeRotate, InputCmdHandler.FromDelegate(OnMoveRotate))
            .Bind(ContentKeyFunctions.ArcadeDrop, InputCmdHandler.FromDelegate(OnMoveDrop))
            .Register<SharedBlockGameArcadeSystem>();
    }

    private void OnArcadeChangedState(Entity<BlockGameArcadeComponent> ent, ref ArcadeChangedStateEvent args)
    {
        if (args.NewState != ArcadeGameState.Game)
            return;

        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateCooldown;

        ent.Comp.Cells = new BlockGameArcadeCell[(ent.Comp.Size.X + ent.Comp.BufferWidth) * ent.Comp.Size.Y];
        Array.Fill(ent.Comp.Cells, BlockGameArcadeCell.Empty);

        // TODO: Use RandomPredicted https://github.com/space-wizards/RobustToolbox/pull/5849
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);
        RandomHelpers.Shuffle(rand, ent.Comp.PiecesBag);

        DirtyFields(ent.AsNullable(), null, nameof(BlockGameArcadeComponent.NextUpdate), nameof(BlockGameArcadeComponent.Cells), nameof(BlockGameArcadeComponent.PiecesBag));
    }

    private void OnMoveLeft(ICommonSession? session)
    {
        ProcessMoveKeyBind(session, BlockGameArcadeMove.Left);
    }

    private void OnMoveUp(ICommonSession? session)
    {
        ProcessMoveKeyBind(session, BlockGameArcadeMove.Up);
    }

    private void OnMoveDown(ICommonSession? session)
    {
        ProcessMoveKeyBind(session, BlockGameArcadeMove.Down);
    }

    private void OnMoveRotate(ICommonSession? session)
    {
        ProcessMoveKeyBind(session, BlockGameArcadeMove.Rotate);
    }

    private void OnMoveDrop(ICommonSession? session)
    {
        ProcessMoveKeyBind(session, BlockGameArcadeMove.Drop);
    }

    private void ProcessMoveKeyBind(ICommonSession? session, BlockGameArcadeMove direction)
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

        if (blockGame.MoveDirection != BlockGameArcadeMove.None)
            return;

        if (_arcade.GetGameState(arcade) != ArcadeGameState.Game)
            return;

        blockGame.MoveDirection = direction;
        DirtyField(arcade, blockGame, nameof(BlockGameArcadeComponent.MoveDirection));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<BlockGameArcadeComponent, ArcadeComponent>();
        while (query.MoveNext(out var uid, out var blockGame, out var arcade))
        {
            if (_arcade.GetGameState((uid, arcade)) != ArcadeGameState.Game)
                continue;

            if (blockGame.NextUpdate > curTime)
                continue;

            blockGame.NextUpdate += blockGame.UpdateCooldown;
            DirtyField(uid, blockGame, nameof(BlockGameArcadeComponent.NextUpdate));

            if (TryMoveRowLeft((uid, blockGame), 1))
                continue;

            if (TrySpawnNextPiece((uid, blockGame)))
                continue;
        }
    }

    /// <summary>
    ///
    /// </summary>
    private bool TryCoordinatesToCell(Entity<BlockGameArcadeComponent?> ent, int x, int y, [NotNullWhen(true)] out int? cell)
    {
        cell = null;

        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (x < 0 || x > ent.Comp.Size.X)
            return false;

        if (y < 0 || y > ent.Comp.Size.Y)
            return false;

        cell = ent.Comp.Size.X * y + x;

        return true;
    }

    /// <summary>
    ///
    /// </summary>
    private bool TryCellToCoordinates(Entity<BlockGameArcadeComponent?> ent, int cell, [NotNullWhen(true)] out int? x, [NotNullWhen(true)] out int? y)
    {
        x = y = null;

        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (cell < 0 || cell > ent.Comp.Size.X * ent.Comp.Size.Y)
            return false;

        x = cell % ent.Comp.Size.X;
        y = cell / ent.Comp.Size.X;

        return true;
    }

    /// <summary>
    ///
    /// </summary>
    private bool CanRowMoveLeft(Entity<BlockGameArcadeComponent?> ent, int x)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        // We are checking against left row
        var leftRow = x - 1;
        for (var y = 0; y < ent.Comp.Size.Y; y++)
        {
            if (!TryCoordinatesToCell(ent, leftRow, y, out var cell))
                return false;

            if (ent.Comp.Cells[cell.Value] != BlockGameArcadeCell.Empty)
                return false;
        }

        return true;
    }

    /// <summary>
    ///
    /// </summary>
    private bool TryMoveRowLeft(Entity<BlockGameArcadeComponent?> ent, int x)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        // Check if we can move before actually moving
        if (!CanRowMoveLeft(ent, x))
            return false;

        var leftRow = x - 1;
        for (var y = 0; y < ent.Comp.Size.Y; y++)
        {
            if (!TryCoordinatesToCell(ent, x, y, out var cell))
                return false;

            if (!TryCoordinatesToCell(ent, leftRow, y, out var leftCell))
                return false;

            ent.Comp.Cells[leftCell.Value] = ent.Comp.Cells[cell.Value];
        }

        DirtyField(ent, nameof(BlockGameArcadeComponent.Cells));

        return true;
    }

    /// <summary>
    ///
    /// </summary>
    private bool TrySpawnNextPiece(Entity<BlockGameArcadeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        return true;
    }
}
