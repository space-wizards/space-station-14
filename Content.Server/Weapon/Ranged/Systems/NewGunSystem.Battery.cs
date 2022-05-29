using Content.Server.Power.Components;
using Content.Shared.Weapons.Ranged;

namespace Content.Server.Weapon.Ranged;

public sealed partial class GunSystem
{
    protected override void InitializeBattery()
    {
        base.InitializeBattery();
        SubscribeLocalEvent<BatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);
    }

    private void OnBatteryStartup(EntityUid uid, BatteryAmmoProviderComponent component, ComponentStartup args)
    {
        UpdateShots(uid, component);
    }

    private void OnBatteryChargeChange(EntityUid uid, BatteryAmmoProviderComponent component, ChargeChangedEvent args)
    {
        UpdateShots(uid, component);
    }

    private void UpdateShots(EntityUid uid, BatteryAmmoProviderComponent component)
    {
        if (!TryComp<BatteryComponent>(uid, out var battery)) return;
        UpdateShots(component, battery);
    }

    private void UpdateShots(BatteryAmmoProviderComponent component, BatteryComponent battery)
    {
        var shots = (int) (battery.CurrentCharge / component.FireCost);
        var maxShots = (int) (battery.MaxCharge / component.FireCost);

        if (component.Shots != shots || component.MaxShots != maxShots)
        {
            Dirty(component);
        }

        component.Shots = shots;
        component.MaxShots = maxShots;
        UpdateBatteryAppearance(component.Owner, component);
    }

    protected override void TakeCharge(EntityUid uid, BatteryAmmoProviderComponent component)
    {
        if (!TryComp<BatteryComponent>(uid, out var battery)) return;

        battery.CurrentCharge -= component.FireCost;
        UpdateShots(component, battery);
    }
}
