using Content.Shared.Popups;
using Content.Shared.Xenobiology.Components;
using Content.Shared.Xenobiology.Components.Machines;
using Content.Shared.Xenobiology.UI;
using Robust.Shared.Containers;

namespace Content.Shared.Xenobiology.Systems.Machines;

public sealed class CellSequencerSystem : EntitySystem
{
    [Dependency] private readonly CellClientSystem _cellClient = default!;
    [Dependency] private readonly CellServerSystem _cellServer = default!;

    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CellSequencerComponent, CellSequencerUiSyncMessage>(OnSync);
        SubscribeLocalEvent<CellSequencerComponent, CellSequencerUiScanMessage>(OnScan);
        SubscribeLocalEvent<CellSequencerComponent, CellSequencerUiCopyMessage>(OnCopy);

        SubscribeLocalEvent<CellSequencerComponent, CellSequencerUiAddMessage>(OnAdd);
        SubscribeLocalEvent<CellSequencerComponent, CellSequencerUiRemoveMessage>(OnRemove);
    }

    private void OnSync(Entity<CellSequencerComponent> ent, ref CellSequencerUiSyncMessage args)
    {
        Sync(ent);
    }

    private void OnScan(Entity<CellSequencerComponent> ent, ref CellSequencerUiScanMessage args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.DishSlot, out var container))
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-no-slot"), ent, null, PopupType.LargeCaution);
            return;
        }

        if (container.ContainedEntities.Count == 0)
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-no-entity"), ent, null, PopupType.MediumCaution);
            return;
        }

        if (!TryComp<CellContainerComponent>(container.ContainedEntities[0], out var cellContainerComponent))
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-entity-not-valid"), ent, null, PopupType.LargeCaution);
            return;
        }

        if (cellContainerComponent.Cells.Count == 0)
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-entity-is-empty"), ent, null, PopupType.MediumCaution);
            return;
        }

        ent.Comp.SelectedCell = cellContainerComponent.Cells[0];
        Sync(ent);
    }

    private void OnCopy(Entity<CellSequencerComponent> ent, ref CellSequencerUiCopyMessage args)
    {
        if (ent.Comp.SelectedCell is null)
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-no-selected"), ent, null, PopupType.MediumCaution);
            return;
        }
    }

    private void OnAdd(Entity<CellSequencerComponent> ent, ref CellSequencerUiAddMessage args)
    {
        if (!_cellClient.TryGetServer(ent.Owner, out var serverEnt))
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-no-connect"), ent, null, PopupType.MediumCaution);
            return;
        }

        if (ent.Comp.SelectedCell is null)
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-no-selected"), ent, null, PopupType.MediumCaution);
            return;
        }

        _cellServer.RegisterCell(serverEnt.Value.Owner, ent.Owner, ent.Comp.SelectedCell);
        Sync(ent);
    }

    private void OnRemove(Entity<CellSequencerComponent> ent, ref CellSequencerUiRemoveMessage args)
    {
        if (!_cellClient.TryGetServer(ent.Owner, out var serverEnt))
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-no-connect"), ent, null, PopupType.MediumCaution);
            return;
        }

        if (ent.Comp.SelectedCell is null)
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-no-selected"), ent, null, PopupType.MediumCaution);
            return;
        }

        _cellServer.RemoveCell(serverEnt.Value.Owner, ent.Owner, ent.Comp.SelectedCell);
        Sync(ent);
    }

    private void Sync(Entity<CellSequencerComponent> ent)
    {
        if (!_cellClient.TryGetServer(ent.Owner, out var serverEnt))
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-no-connect"), ent, null, PopupType.MediumCaution);
            return;
        }

        var state = new CellSequencerUiState(ent.Comp.SelectedCell, serverEnt.Value.Comp.Cells);
        _userInterface.SetUiState(ent.Owner, CellSequencerUiKey.Key, state);
    }
}
