using Content.Server.Power.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeBattery()
    {
        base.InitializeBattery();

        // Hitscan
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);

        // Projectile
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);
    }

    private void OnBatteryStartup(EntityUid uid, BatteryAmmoProviderComponent component, ComponentStartup args)
    {
        UpdateShots(uid, component);
    }

    private void OnBatteryChargeChange(EntityUid uid, BatteryAmmoProviderComponent component, ref ChargeChangedEvent args)
    {
        UpdateShots(uid, component, args.Charge, args.MaxCharge);
    }

    private void UpdateShots(EntityUid uid, BatteryAmmoProviderComponent component)
    {
        if (!TryComp<BatteryComponent>(uid, out var battery))
            return;

        UpdateShots(uid, component, battery.CurrentCharge, battery.MaxCharge);
    }

    private void UpdateShots(EntityUid uid, BatteryAmmoProviderComponent component, float charge, float maxCharge)
    {
        var shots = (int) (charge / component.FireCost);
        var maxShots = (int) (maxCharge / component.FireCost);

        if (component.Shots != shots || component.Capacity != maxShots)
        {
            Dirty(uid, component);
        }

        component.Shots = shots;
        component.Capacity = maxShots;
        UpdateBatteryAppearance(uid, component);
    }

    protected override void TakeCharge(EntityUid uid, BatteryAmmoProviderComponent component)
    {
        // Will raise ChargeChangedEvent
        _battery.UseCharge(uid, component.FireCost);
    }
}
