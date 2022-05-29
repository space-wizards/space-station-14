using Content.Server.Chemistry.Components;
using Content.Server.Power.Components;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ChemistrySystem
{
    private void InitializeReagentDispenser()
    {
        SubscribeLocalEvent<ReagentDispenserComponent, PowerChangedEvent>(OnReagentDispenserPower);
    }

    private static void OnReagentDispenserPower(EntityUid uid, ReagentDispenserComponent component, PowerChangedEvent args)
    {
        component.OnPowerChanged();
    }
}
