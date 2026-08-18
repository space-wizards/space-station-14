using Content.Client.Chemistry.UI;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.EntitySystems;

/// <summary>
/// This handles client-side logic for ChemMasters.
/// <seealso cref="ChemMasterComponent"/>
/// </summary>
public sealed partial class ChemMasterSystem : SharedChemMasterSystem
{
    [Dependency] private UserInterfaceSystem _ui = null!;

    protected override void UpdateUi(Entity<ChemMasterComponent> ent)
    {
        if (!_ui.TryGetOpenUi(ent.Owner, ChemMasterUiKey.Key, out var bui))
            return;

        bui.Update();
    }

    protected override void UpdateUiLabels(Entity<ChemMasterComponent> ent)
    {
        if (!_ui.TryGetOpenUi(ent.Owner, ChemMasterUiKey.Key, out var bui)
            || bui is not ChemMasterBoundUserInterface chemMasterBui)
            return;

        chemMasterBui.UpdateUiLabels();
    }

    [SubscribeLocalEvent]
    private void OnChemMasterState(Entity<ChemMasterComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }
}
