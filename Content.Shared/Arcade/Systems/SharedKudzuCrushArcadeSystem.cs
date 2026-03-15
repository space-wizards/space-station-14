using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Enums;
using Content.Shared.Arcade.Events;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Arcade.Systems;

/// <summary>
///
/// </summary>
public abstract partial class SharedKudzuCrushArcadeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedArcadeSystem _arcade = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KudzuCrushArcadeComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<KudzuCrushArcadeComponent, ArcadeChangedStateEvent>(OnArcadeChangedState);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<KudzuCrushArcadeComponent, ArcadeComponent>();
        while (query.MoveNext(out var uid, out var kudzuCrush, out var arcade))
        {
            if (arcade.State != ArcadeGameState.Game)
                continue;

            if (kudzuCrush.NextUpdate > curTime)
                continue;

            kudzuCrush.NextUpdate += kudzuCrush.UpdateCooldown;

            if (TrySpawnNextPiece((uid, kudzuCrush)))
                continue;

            if (TryMoveFallingPieceDown((uid, kudzuCrush)))
                TryFreezeFallingPiece((uid, kudzuCrush));
        }
    }

    private void OnComponentInit(Entity<KudzuCrushArcadeComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Grid = new KudzuCrushArcadeCell[ent.Comp.GridSize.X * ent.Comp.GridSize.Y];

    }

    private void OnArcadeChangedState(Entity<KudzuCrushArcadeComponent> ent, ref ArcadeChangedStateEvent args)
    {
        if (args.NewState != ArcadeGameState.NewGame)
            return;

        ent.Comp.FallingPieceCells = null;
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateCooldown;
        ent.Comp.Grid.AsSpan().Clear();
        ent.Comp.NextBagPiece = 0;

        // TODO: Use RandomPredicted https://github.com/space-wizards/RobustToolbox/pull/5849
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);
        RandomHelpers.Shuffle(rand, ent.Comp.PiecesBag);

        CreateUIGrid(ent);
    }

    /// <summary>
    ///
    /// </summary>
    private bool TrySpawnNextPiece(Entity<KudzuCrushArcadeComponent> ent)
    {
        if (ent.Comp.FallingPieceCells != null)
            return false;

        var protoId = ent.Comp.PiecesBag[ent.Comp.NextBagPiece];
        if (!_prototypeManager.TryIndex(protoId, out var fallingPiece))
        {
            Log.Error($"Invalid piece prototype {protoId} for {ToPrettyString(ent)}");
            return false;
        }

        ent.Comp.FallingPieceCells = new int[fallingPiece.Cells.Length];

        var width = ent.Comp.GridSize.X;
        var gridWidthCenter = width / 2;

        var indexLimit = ent.Comp.Grid.Length - 1;

        for (var i = 0; i < fallingPiece.Cells.Length; i++)
        {
            var index = fallingPiece.Cells[i].Y * width + gridWidthCenter + fallingPiece.Cells[i].X;
            if (index < 0 || index > indexLimit)
            {
                Log.Error($"Piece {protoId} can't fit into grid of {ToPrettyString(ent)}");
                ent.Comp.FallingPieceCells = null;

                return false;
            }

            ent.Comp.FallingPieceCells[i] = index;
            ent.Comp.Grid[index] = KudzuCrushArcadeCell.Block;

            UpdateUIGridCell(ent, index, KudzuCrushArcadeCell.Block);
        }

        if (++ent.Comp.NextBagPiece >= ent.Comp.PiecesBag.Length - 1)
        {
            ent.Comp.NextBagPiece = 0;

            // TODO: Use RandomPredicted https://github.com/space-wizards/RobustToolbox/pull/5849
            var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
            var rand = new System.Random(seed);
            RandomHelpers.Shuffle(rand, ent.Comp.PiecesBag);

            //DirtyField(ent.AsNullable(), nameof(KudzuCrushArcadeComponent.PiecesBag));
        }

        //DirtyFields(ent.AsNullable(), null,
        //    nameof(KudzuCrushArcadeComponent.NextBagPiece),
        //    nameof(KudzuCrushArcadeComponent.FallingPieceCells),
        //    nameof(KudzuCrushArcadeComponent.Grid),
        //    nameof(KudzuCrushArcadeComponent.FallingPieceColor));

        return true;
    }

    /// <summary>
    ///
    /// </summary>
    private bool TryMoveFallingPieceDown(Entity<KudzuCrushArcadeComponent> ent)
    {
        if (ent.Comp.FallingPieceCells == null)
            return false;

        for (var i = 0; i < ent.Comp.FallingPieceCells.Length; i++)
        {
            var index = ent.Comp.FallingPieceCells[i];
            ent.Comp.Grid[index] = KudzuCrushArcadeCell.Empty;
            UpdateUIGridCell(ent, index, KudzuCrushArcadeCell.Empty);
        }

        for (var i = 0; i < ent.Comp.FallingPieceCells.Length; i++)
        {
            var newIndex = ent.Comp.FallingPieceCells[i] + ent.Comp.GridSize.X;
            ent.Comp.Grid[newIndex] = KudzuCrushArcadeCell.Block;
            UpdateUIGridCell(ent, newIndex, KudzuCrushArcadeCell.Block);

            ent.Comp.FallingPieceCells[i] = newIndex;
        }

        // DirtyFields(ent.AsNullable(), null, nameof(KudzuCrushArcadeComponent.Grid), nameof(KudzuCrushArcadeComponent.FallingPieceCells));

        return true;
    }

    /// <summary>
    ///
    /// </summary>
    private bool TryFreezeFallingPiece(Entity<KudzuCrushArcadeComponent> ent)
    {
        if (ent.Comp.FallingPieceCells == null)
            return false;

        for (var i = 0; i < ent.Comp.FallingPieceCells.Length; i++)
        {
            var bottomIndex = ent.Comp.FallingPieceCells[i] + ent.Comp.GridSize.X;
            if (bottomIndex >= ent.Comp.Grid.Length || ent.Comp.Grid[bottomIndex] == KudzuCrushArcadeCell.Block && !ent.Comp.FallingPieceCells.Contains(bottomIndex))
            {
                ent.Comp.FallingPieceCells = null;

                // DirtyFields(ent.AsNullable(), null, nameof(KudzuCrushArcadeComponent.FallingPieceCells), nameof(KudzuCrushArcadeComponent.FallingPieceColor));

                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///
    /// </summary>
    private bool TryShiftRowDown(Entity<KudzuCrushArcadeComponent> ent, int y)
    {
        if (y < 1 || y >= ent.Comp.GridSize.Y - 1)
            return false;

        var srcRowStart = y * ent.Comp.GridSize.X;
        var dstRowStart = (y + 1) * ent.Comp.GridSize.X;

        var moved = false;
        for (var x = 0; x < ent.Comp.GridSize.X; x++)
        {
            var srcIndex = srcRowStart + x;
            if (ent.Comp.Grid[srcIndex] != KudzuCrushArcadeCell.Empty)
                continue;

            var dstIndex = dstRowStart + x;
            if (ent.Comp.Grid[dstIndex] == KudzuCrushArcadeCell.Block)
                continue;

            ent.Comp.Grid[dstIndex] = KudzuCrushArcadeCell.Block;
            ent.Comp.Grid[srcIndex] = KudzuCrushArcadeCell.Empty;

            UpdateUIGridCell(ent, dstIndex, KudzuCrushArcadeCell.Block);
            UpdateUIGridCell(ent, srcIndex, KudzuCrushArcadeCell.Empty);

            moved = true;
        }

        //if (moved)
        //    DirtyField(ent.AsNullable(), nameof(KudzuCrushArcadeComponent.Grid));

        return moved;
    }

    /// <summary>
    ///
    /// </summary>
    protected virtual void CreateUIGrid(Entity<KudzuCrushArcadeComponent> ent) { }

    /// <summary>
    ///
    /// </summary>
    protected virtual void UpdateUIGridCell(Entity<KudzuCrushArcadeComponent> ent, int index, KudzuCrushArcadeCell cell) { }
}
