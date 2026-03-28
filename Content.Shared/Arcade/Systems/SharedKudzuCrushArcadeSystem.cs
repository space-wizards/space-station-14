using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Enums;
using Content.Shared.Arcade.Events;
using Content.Shared.Arcade.Messages.KudzuCrush;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Arcade.Systems;

/// <summary>
///
/// </summary>
public abstract partial class SharedKudzuCrushArcadeSystem : EntitySystem
{
    [Dependency] private readonly SharedArcadeSystem _arcade = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KudzuCrushArcadeComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<KudzuCrushArcadeComponent, ArcadeChangedStateEvent>(OnArcadeChangedState);

        SubscribeLocalEvent<KudzuCrushArcadeComponent, KudzuCrushArcadeActionLeftMessage>(OnActionLeft);
        SubscribeLocalEvent<KudzuCrushArcadeComponent, KudzuCrushArcadeActionRightMessage>(OnActionRight);
        SubscribeLocalEvent<KudzuCrushArcadeComponent, KudzuCrushArcadeActionDropMessage>(OnActionDrop);
        SubscribeLocalEvent<KudzuCrushArcadeComponent, KudzuCrushArcadeActionRotateMessage>(OnActionRotate);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = Timing.CurTime;
        var query = EntityQueryEnumerator<KudzuCrushArcadeComponent, ArcadeComponent>();
        while (query.MoveNext(out var uid, out var kudzuCrush, out var arcade))
        {
            if (arcade.State != ArcadeGameState.Game)
                continue;

            if (kudzuCrush.NextUpdate > curTime)
                continue;

            kudzuCrush.NextUpdate += kudzuCrush.UpdateCooldown;
            DirtyField(uid, kudzuCrush, nameof(KudzuCrushArcadeComponent.NextUpdate));

            var ent = (uid, kudzuCrush);

            if (TrySpawnNextPiece(ent))
                continue;

            ProcessAction(ent);

            TryMoveFallingPieceDown(ent);
        }
    }

    private void OnComponentInit(Entity<KudzuCrushArcadeComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Grid = new KudzuCrushArcadeCell[ent.Comp.GridSize.X * ent.Comp.GridSize.Y];
        DirtyField(ent.AsNullable(), nameof(KudzuCrushArcadeComponent.Grid));

        UpdateUi(ent);
    }

    private void OnArcadeChangedState(Entity<KudzuCrushArcadeComponent> ent, ref ArcadeChangedStateEvent args)
    {
        if (args.NewState != ArcadeGameState.NewGame)
            return;

        ent.Comp.PieceCells = null;
        ent.Comp.NextUpdate = Timing.CurTime + ent.Comp.UpdateCooldown;
        ent.Comp.Grid.AsSpan().Clear();
        ent.Comp.NextBagPiece = 0;
        ent.Comp.NextAction = KudzuCrushArcadeAction.None;

        // TODO: Use RandomPredicted https://github.com/space-wizards/RobustToolbox/pull/5849
        SharedRandomExtensions.PredictedRandom(Timing, GetNetEntity(ent)).Shuffle(ent.Comp.PiecesBag);

        DirtyFields(ent.AsNullable(), null,
            nameof(KudzuCrushArcadeComponent.PieceCells),
            nameof(KudzuCrushArcadeComponent.NextUpdate),
            nameof(KudzuCrushArcadeComponent.Grid),
            nameof(KudzuCrushArcadeComponent.NextBagPiece),
            nameof(KudzuCrushArcadeComponent.NextAction),
            nameof(KudzuCrushArcadeComponent.PiecesBag));

        UpdateUi(ent);
    }

    private void OnActionLeft(Entity<KudzuCrushArcadeComponent> ent, ref KudzuCrushArcadeActionLeftMessage args)
    {
        if (ent.Comp.IsPieceFalling)
            return;

        if (ent.Comp.PieceCells == null)
            return;

        if (ent.Comp.NextAction != KudzuCrushArcadeAction.None)
            return;

        if (!_arcade.IsPlayer(ent.Owner, args.Actor))
            return;

        ent.Comp.NextAction = KudzuCrushArcadeAction.Left;
        DirtyField(ent.AsNullable(), nameof(KudzuCrushArcadeComponent.NextAction));
    }

    private void OnActionRight(Entity<KudzuCrushArcadeComponent> ent, ref KudzuCrushArcadeActionRightMessage args)
    {
        if (ent.Comp.IsPieceFalling)
            return;

        if (ent.Comp.PieceCells == null)
            return;

        if (ent.Comp.NextAction != KudzuCrushArcadeAction.None)
            return;

        if (!_arcade.IsPlayer(ent.Owner, args.Actor))
            return;

        ent.Comp.NextAction = KudzuCrushArcadeAction.Right;
        DirtyField(ent.AsNullable(), nameof(KudzuCrushArcadeComponent.NextAction));
    }

    private void OnActionDrop(Entity<KudzuCrushArcadeComponent> ent, ref KudzuCrushArcadeActionDropMessage args)
    {
        if (ent.Comp.IsPieceFalling)
            return;

        if (ent.Comp.PieceCells == null)
            return;

        if (ent.Comp.NextAction != KudzuCrushArcadeAction.None)
            return;

        if (!_arcade.IsPlayer(ent.Owner, args.Actor))
            return;

        ent.Comp.NextAction = KudzuCrushArcadeAction.Drop;
        DirtyField(ent.AsNullable(), nameof(KudzuCrushArcadeComponent.NextAction));
    }

    private void OnActionRotate(Entity<KudzuCrushArcadeComponent> ent, ref KudzuCrushArcadeActionRotateMessage args)
    {
        if (ent.Comp.IsPieceFalling)
            return;

        if (ent.Comp.PieceCells == null)
            return;

        if (ent.Comp.NextAction != KudzuCrushArcadeAction.None)
            return;

        if (!_arcade.IsPlayer(ent.Owner, args.Actor))
            return;

        ent.Comp.NextAction = KudzuCrushArcadeAction.Rotate;
        DirtyField(ent.AsNullable(), nameof(KudzuCrushArcadeComponent.NextAction));
    }

    /// <summary>
    ///
    /// </summary>
    private bool TrySpawnNextPiece(Entity<KudzuCrushArcadeComponent> ent)
    {
        if (ent.Comp.PieceCells != null)
            return false;

        var protoId = ent.Comp.PiecesBag[ent.Comp.NextBagPiece];
        if (!_prototypeManager.TryIndex(protoId, out var fallingPiece))
        {
            Log.Error($"Invalid piece prototype {protoId} for {ToPrettyString(ent)}");
            return false;
        }

        ent.Comp.PieceCells = new int[fallingPiece.Cells.Length];

        var width = ent.Comp.GridSize.X;
        var gridWidthCenter = width / 2;

        var indexLimit = ent.Comp.Grid.Length - 1;

        for (var i = 0; i < fallingPiece.Cells.Length; i++)
        {
            var index = fallingPiece.Cells[i].Y * width + gridWidthCenter + fallingPiece.Cells[i].X;
            if (index < 0 || index > indexLimit)
            {
                Log.Error($"Piece {protoId} can't fit into grid of {ToPrettyString(ent)}");
                ent.Comp.PieceCells = null;

                return false;
            }

            ent.Comp.PieceCells[i] = index;
            ent.Comp.Grid[index] = KudzuCrushArcadeCell.Block;
        }

        if (++ent.Comp.NextBagPiece >= ent.Comp.PiecesBag.Length - 1)
        {
            ent.Comp.NextBagPiece = 0;

            // TODO: Use RandomPredicted https://github.com/space-wizards/RobustToolbox/pull/5849
            SharedRandomExtensions.PredictedRandom(Timing, GetNetEntity(ent)).Shuffle(ent.Comp.PiecesBag);

            DirtyField(ent.AsNullable(), nameof(KudzuCrushArcadeComponent.PiecesBag));
        }

        DirtyFields(ent.AsNullable(), null,
            nameof(KudzuCrushArcadeComponent.NextBagPiece),
            nameof(KudzuCrushArcadeComponent.PieceCells),
            nameof(KudzuCrushArcadeComponent.Grid));

        UpdateUi(ent);

        return true;
    }

    /// <summary>
    ///
    /// </summary>
    private void ProcessAction(Entity<KudzuCrushArcadeComponent> ent)
    {
        switch (ent.Comp.NextAction)
        {
            case KudzuCrushArcadeAction.Left:
                TryMovePieceLeft(ent);
                break;

            case KudzuCrushArcadeAction.Right:
                TryMovePieceRight(ent);
                break;

            case KudzuCrushArcadeAction.Drop:
                break;

            case KudzuCrushArcadeAction.Rotate:
                break;
        }

        ent.Comp.NextAction = KudzuCrushArcadeAction.None;
        DirtyField(ent.AsNullable(), nameof(KudzuCrushArcadeComponent.NextAction));
    }

    /// <summary>
    ///
    /// </summary>
    private bool TryFreezeFallingPiece(Entity<KudzuCrushArcadeComponent> ent)
    {
        if (!ent.Comp.IsPieceFalling)
            return false;

        if (ent.Comp.PieceCells == null)
            return false;

        for (var i = 0; i < ent.Comp.PieceCells.Length; i++)
        {
            var bottomIndex = ent.Comp.PieceCells[i] + ent.Comp.GridSize.X;
            if (bottomIndex >= ent.Comp.Grid.Length || ent.Comp.Grid[bottomIndex] == KudzuCrushArcadeCell.Block && !ent.Comp.PieceCells.Contains(bottomIndex))
            {
                ent.Comp.PieceCells = null;
                DirtyField(ent.AsNullable(), nameof(KudzuCrushArcadeComponent.PieceCells));

                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///
    /// </summary>
    private bool TryMovePieceLeft(Entity<KudzuCrushArcadeComponent> ent)
    {
        if (ent.Comp.PieceCells == null)
            return false;

        if (ent.Comp.PieceCells[0] % ent.Comp.GridSize.X == 0)
            return false;

        for (var i = 0; i < ent.Comp.PieceCells.Length; i++)
        {
            var index = ent.Comp.PieceCells[i];
            ent.Comp.Grid[index] = KudzuCrushArcadeCell.Empty;
        }

        for (var i = 0; i < ent.Comp.PieceCells.Length; i++)
        {
            var newIndex = --ent.Comp.PieceCells[i];
            ent.Comp.Grid[newIndex] = KudzuCrushArcadeCell.Block;
        }

        DirtyFields(ent.AsNullable(), null, nameof(KudzuCrushArcadeComponent.Grid), nameof(KudzuCrushArcadeComponent.PieceCells));

        UpdateUi(ent);

        return true;
    }

    /// <summary>
    ///
    /// </summary>
    private bool TryMovePieceRight(Entity<KudzuCrushArcadeComponent> ent)
    {
        if (ent.Comp.PieceCells == null)
            return false;

        if ((ent.Comp.PieceCells[^1] + 1) % ent.Comp.GridSize.X == 0)
            return false;

        for (var i = 0; i < ent.Comp.PieceCells.Length; i++)
        {
            var index = ent.Comp.PieceCells[i];
            ent.Comp.Grid[index] = KudzuCrushArcadeCell.Empty;
        }

        for (var i = 0; i < ent.Comp.PieceCells.Length; i++)
        {
            var newIndex = ++ent.Comp.PieceCells[i];
            ent.Comp.Grid[newIndex] = KudzuCrushArcadeCell.Block;
        }

        DirtyFields(ent.AsNullable(), null, nameof(KudzuCrushArcadeComponent.Grid), nameof(KudzuCrushArcadeComponent.PieceCells));

        UpdateUi(ent);

        return true;
    }

    /// <summary>
    ///
    /// </summary>
    private bool TryMoveFallingPieceDown(Entity<KudzuCrushArcadeComponent> ent)
    {
        if (!ent.Comp.IsPieceFalling)
            return false;

        if (ent.Comp.PieceCells == null)
            return false;

        if (TryFreezeFallingPiece(ent))
            return false;

        for (var i = 0; i < ent.Comp.PieceCells.Length; i++)
        {
            var index = ent.Comp.PieceCells[i];
            ent.Comp.Grid[index] = KudzuCrushArcadeCell.Empty;
        }

        for (var i = 0; i < ent.Comp.PieceCells.Length; i++)
        {
            var newIndex = ent.Comp.PieceCells[i] + ent.Comp.GridSize.X;
            ent.Comp.Grid[newIndex] = KudzuCrushArcadeCell.Block;

            ent.Comp.PieceCells[i] = newIndex;
        }

        DirtyFields(ent.AsNullable(), null, nameof(KudzuCrushArcadeComponent.Grid), nameof(KudzuCrushArcadeComponent.PieceCells));

        UpdateUi(ent);

        return true;
    }

    protected virtual void UpdateUi(Entity<KudzuCrushArcadeComponent> ent) { }
}
