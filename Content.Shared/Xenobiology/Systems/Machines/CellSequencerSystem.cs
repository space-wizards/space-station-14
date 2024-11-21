using System.Linq;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Xenobiology.Components.Container;
using Content.Shared.Xenobiology.Components.Machines;
using Content.Shared.Xenobiology.Systems.Machines.Connection;
using Content.Shared.Xenobiology.UI;
using Robust.Shared.Containers;

namespace Content.Shared.Xenobiology.Systems.Machines;

public sealed class CellSequencerSystem : EntitySystem
{
    [Dependency] private readonly CellClientSystem _cellClient = default!;
    [Dependency] private readonly CellServerSystem _cellServer = default!;

    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;

    [Dependency] private readonly SharedCellSystem _cell = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CellSequencerComponent, EntInsertedIntoContainerMessage>(OnInsertIntoContainer);
        SubscribeLocalEvent<CellSequencerComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);

        SubscribeLocalEvent<CellSequencerComponent, MaterialAmountChangedEvent>(OnMaterialAmountChanged);

        SubscribeLocalEvent<CellSequencerComponent, CellSequencerUiSyncMessage>(OnSync);

        SubscribeLocalEvent<CellSequencerComponent, CellSequencerUiAddMessage>(OnAdd);
        SubscribeLocalEvent<CellSequencerComponent, CellSequencerUiRemoveMessage>(OnRemove);
        SubscribeLocalEvent<CellSequencerComponent, CellSequencerUiReplaceMessage>(OnReplace);
    }

    private void OnInsertIntoContainer(Entity<CellSequencerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateInsideCellContainers(ent);
        UpdateInsideCells(ent);
        UpdateUI(ent);
    }

    private void OnRemovedFromContainer(Entity<CellSequencerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateInsideCellContainers(ent);
        UpdateInsideCells(ent);
        UpdateUI(ent);
    }

    private void OnMaterialAmountChanged(Entity<CellSequencerComponent> ent, ref MaterialAmountChangedEvent args)
    {
        ent.Comp.MaterialAmount = _materialStorage.GetMaterialAmount(ent, ent.Comp.RequiredMaterial);
        UpdateUI(ent);
    }

    private void OnSync(Entity<CellSequencerComponent> ent, ref CellSequencerUiSyncMessage args)
    {
        UpdateUI(ent);
    }

    private void OnAdd(Entity<CellSequencerComponent> ent, ref CellSequencerUiAddMessage args)
    {
        if (!_cellClient.TryGetServer(ent.Owner, out var serverEnt))
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-no-connect"), ent, null, PopupType.MediumCaution);
            return;
        }

        if (args.Cell is null)
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-no-selected"), ent, null, PopupType.MediumCaution);
            return;
        }

        _cellServer.AddCell(serverEnt.Value.Owner, ent.Owner, args.Cell);
        UpdateUI(ent);
    }

    private void OnRemove(Entity<CellSequencerComponent> ent, ref CellSequencerUiRemoveMessage args)
    {
        if (args.Cell is null)
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-no-selected"), ent, null, PopupType.MediumCaution);
            return;
        }

        if (!args.Remote)
        {
            foreach (var cell in ent.Comp.Cells)
            {
                if (cell != args.Cell)
                    continue;

                ent.Comp.Cells.Remove(args.Cell);
                UpdateUI(ent);
                return;
            }

            return;
        }

        if (!_cellClient.TryGetServer(ent.Owner, out var serverEnt))
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-no-connect"), ent, null, PopupType.MediumCaution);
            return;
        }

        if (!serverEnt.Value.Comp.Cells.Contains(args.Cell))
            return;

        _cellServer.RemoveCell(serverEnt.Value.Owner, ent.Owner, args.Cell);
        UpdateUI(ent);
    }

    private void OnReplace(Entity<CellSequencerComponent> ent, ref CellSequencerUiReplaceMessage args)
    {
        if (args.Cell is null)
            return;

        if (!_cellClient.TryGetCells(ent.Owner, out var cells))
            return;

        if (!cells.Contains(args.Cell))
            return;

        if (ent.Comp.MaterialAmount < args.Cell.Cost)
            return;

        if (!_materialStorage.TrySetMaterialAmount(ent, ent.Comp.RequiredMaterial, ent.Comp.MaterialAmount - args.Cell.Cost))
            return;

        foreach (var container in ent.Comp.CellContainers)
        {
            _cell.ClearCells(container.Owner);
            _cell.AddCell(container.Owner, args.Cell);
        }

        UpdateInsideCells(ent);
        UpdateUI(ent);
    }

    private void UpdateUI(Entity<CellSequencerComponent> ent)
    {
        if (!_cellClient.TryGetServer(ent.Owner, out var serverEnt))
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-no-connect"), ent, null, PopupType.MediumCaution);
            return;
        }

        var hasContainer = _container.HasContainer(ent, ent.Comp.DishSlot, null);
        var state = new CellSequencerUiState(ent.Comp.Cells, serverEnt.Value.Comp.Cells, ent.Comp.MaterialAmount, hasContainer);
        _userInterface.SetUiState(ent.Owner, CellSequencerUiKey.Key, state);
    }

    private void UpdateInsideCells(Entity<CellSequencerComponent> ent)
    {
        var list = new List<Cell>();
        foreach (var container in ent.Comp.CellContainers)
        {
            foreach (var cell in container.Comp.Cells)
            {
                list.Add(cell);
            }
        }

        ent.Comp.Cells = list;
    }

    private void UpdateInsideCellContainers(Entity<CellSequencerComponent> ent)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.DishSlot, out var container))
            return;

        var list = new List<Entity<CellContainerComponent>>();
        foreach (var entityUid in container.ContainedEntities)
        {
            if (!TryComp<CellContainerComponent>(entityUid, out var cellContainerComponent))
                continue;

            list.Add((entityUid, cellContainerComponent));
        }

        ent.Comp.CellContainers = list;
    }
}
