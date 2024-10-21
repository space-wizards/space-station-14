using System.Linq;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Xenobiology.Components.Machines;
using Content.Shared.Xenobiology.Systems.Machines.Connection;
using Content.Shared.Xenobiology.UI;

namespace Content.Shared.Xenobiology.Systems.Machines;

public sealed class CellularFusionSystem : EntitySystem
{
    [Dependency] private readonly CellClientSystem _cellClient = default!;
    [Dependency] private readonly CellServerSystem _cellServer = default!;

    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CellularFusionComponent, MaterialAmountChangedEvent>(OnMaterialAmountChanged);

        SubscribeLocalEvent<CellularFusionComponent, CellularFusionUiSyncMessage>(OnSync);
        SubscribeLocalEvent<CellularFusionComponent, CellularFusionUiSpliceMessage>(OnSplice);
    }

    private void OnMaterialAmountChanged(Entity<CellularFusionComponent> ent, ref MaterialAmountChangedEvent args)
    {
        ent.Comp.MaterialAmount = _materialStorage.GetMaterialAmount(ent, ent.Comp.RequiredMaterial);
        UpdateUI(ent);
    }

    private void OnSync(Entity<CellularFusionComponent> ent, ref CellularFusionUiSyncMessage args)
    {
        UpdateUI(ent);
    }

    private void OnSplice(Entity<CellularFusionComponent> ent, ref CellularFusionUiSpliceMessage args)
    {
        if (!_cellClient.TryGetCells((ent, null), out var cells))
            return;

        if (!cells.Contains(args.CellA) || !cells.Contains(args.CellB))
            return;

        var cost = SharedCellSystem.GetMergedCost(args.CellA, args.CellB);
        if (cost > ent.Comp.MaterialAmount)
            return;

        if (!_materialStorage.TrySetMaterialAmount(ent, ent.Comp.RequiredMaterial, ent.Comp.MaterialAmount - cost))
            return;

        _cellServer.AddCell(ent, SharedCellSystem.Merge(args.CellA, args.CellB));
    }

    private void UpdateUI(Entity<CellularFusionComponent> ent)
    {
        if (!_cellClient.TryGetServer(ent.Owner, out var serverEnt))
        {
            _popup.PopupPredicted(Loc.GetString("cellular-fusion-no-connect"), ent, null, PopupType.MediumCaution);
            return;
        }

        var state = new CellularFusionUiState(serverEnt.Value.Comp.Cells, ent.Comp.MaterialAmount);
        _userInterface.SetUiState(ent.Owner, CellularFusionUiKey.Key, state);
    }
}
