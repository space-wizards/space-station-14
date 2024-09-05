using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Xenobiology.Components.Machines;
using Content.Shared.Xenobiology.UI;
using Robust.Shared.Containers;

namespace Content.Shared.Xenobiology.Systems.Machines;

public sealed class CellularFusionSystem : EntitySystem
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

        SubscribeLocalEvent<CellularFusionComponent, CellularFusionUiSyncMessage>(OnSync);
    }

    private void OnSync(Entity<CellularFusionComponent> ent, ref CellularFusionUiSyncMessage args)
    {
        UpdateUI(ent);
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
