using Content.Server.Chemistry.Components;
using Content.Server.Power.Components;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ChemistrySystem
{
    private void InitializeChemMaster()
    {
        SubscribeLocalEvent<ChemMasterComponent, PowerChangedEvent>(OnChemMasterPowerChange);
    }

    private static void OnChemMasterPowerChange(EntityUid uid, ChemMasterComponent component, ref PowerChangedEvent args)
    {
        component.UpdateUserInterface();
    }
}
