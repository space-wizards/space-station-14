using Content.Shared.Ame;
using Content.Shared.Ame.Components;
using Content.Shared.Ame.Systems;
using Content.Shared.Power.Components;

namespace Content.Server.Ame;

public sealed partial class AmeControllerSystem : SharedAmeControllerSystem
{
    public override void UpdateUi(Entity<AmeControllerComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (!UISystem.HasUi(ent.Owner, AmeControllerUiKey.Key))
            return;

        var state = GetUiState(ent!);
        UISystem.SetUiState(ent.Owner, AmeControllerUiKey.Key, state);

        ent.Comp.NextUIUpdate += ent.Comp.UpdateUIPeriod;
    }

    private AmeControllerBoundUserInterfaceState GetUiState(Entity<AmeControllerComponent> ent)
    {
        float currentPowerSupply = 0;
        if (TryComp<PowerSupplierComponent>(ent, out var powerOutlet))
            currentPowerSupply = powerOutlet.CurrentSupply / 1000;

        return new AmeControllerBoundUserInterfaceState(currentPowerSupply);
    }
}
