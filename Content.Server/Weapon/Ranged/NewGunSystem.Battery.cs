using Content.Server.Power.Components;
using Content.Shared.Weapons.Ranged;

namespace Content.Server.Weapon.Ranged;

public sealed partial class NewGunSystem
{
    protected override void InitializeBattery()
    {
        base.InitializeBattery();
        SubscribeLocalEvent<BatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);
    }

    private void OnBatteryStartup(EntityUid uid, BatteryAmmoProviderComponent component, ComponentStartup args)
    {
        if (TryComp<BatteryComponent>(uid, out var battery))
        {
            SetShots(component, battery);
        }
    }

    private void OnBatteryChargeChange(EntityUid uid, BatteryAmmoProviderComponent component, ChargeChangedEvent args)
    {
        if (!TryComp<BatteryComponent>(uid, out var battery)) return;

        SetShots(component, battery);
    }

    private void SetShots(BatteryAmmoProviderComponent component, BatteryComponent battery)
    {
        var shots = (int) (battery.CurrentCharge / component.FireCost);
        var maxShots = (int) (battery.MaxCharge / component.FireCost);

        if (component.Shots != shots || component.MaxShots != maxShots)
        {
            Dirty(component);
        }

        component.Shots = shots;
        component.MaxShots = maxShots;
    }
}
