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

        SubscribeLocalEvent<CellSequencerComponent, EntInsertedIntoContainerMessage>(OnInsertIntoContainer);
        SubscribeLocalEvent<CellSequencerComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);

        SubscribeLocalEvent<CellSequencerComponent, CellSequencerUiSyncMessage>(OnSync);

        SubscribeLocalEvent<CellSequencerComponent, CellSequencerUiCopyMessage>(OnCopy);
        SubscribeLocalEvent<CellSequencerComponent, CellSequencerUiAddMessage>(OnAdd);
        SubscribeLocalEvent<CellSequencerComponent, CellSequencerUiRemoveMessage>(OnRemove);
    }

    private void OnInsertIntoContainer(Entity<CellSequencerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateInsideCellContainers(ent);
        UpdateInsideCells(ent);
        Sync(ent);
    }

    private void OnRemovedFromContainer(Entity<CellSequencerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateInsideCellContainers(ent);
        UpdateInsideCells(ent);
        Sync(ent);
    }

    private void OnSync(Entity<CellSequencerComponent> ent, ref CellSequencerUiSyncMessage args)
    {
        Sync(ent);
    }

    private void OnCopy(Entity<CellSequencerComponent> ent, ref CellSequencerUiCopyMessage args)
    {
        if (args.Cell is null)
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

        if (args.Cell is null)
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-no-selected"), ent, null, PopupType.MediumCaution);
            return;
        }

        _cellServer.RegisterCell(serverEnt.Value.Owner, ent.Owner, args.Cell);
        Sync(ent);
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
                Sync(ent);
                return;
            }

            return;
        }

        if (!_cellClient.TryGetServer(ent.Owner, out var serverEnt))
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-no-connect"), ent, null, PopupType.MediumCaution);
            return;
        }

        foreach (var cell in serverEnt.Value.Comp.Cells)
        {
            if (cell != args.Cell)
                continue;

            _cellServer.RemoveCell(serverEnt.Value.Owner, ent.Owner, args.Cell);
            Sync(ent);
            return;
        }
    }

    private void Sync(Entity<CellSequencerComponent> ent)
    {
        if (!_cellClient.TryGetServer(ent.Owner, out var serverEnt))
        {
            _popup.PopupPredicted(Loc.GetString("cell-sequencer-no-connect"), ent, null, PopupType.MediumCaution);
            return;
        }

        var state = new CellSequencerUiState(ent.Comp.Cells, serverEnt.Value.Comp.Cells);
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
