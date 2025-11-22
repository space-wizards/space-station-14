using Content.Shared.Power;
using Content.Shared.PowerCell.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeBattery()
    {
        base.InitializeBattery();

        // Hitscan
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, PowerCellChangedEvent>(OnPowerCellChanged);

        // Projectile
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, PowerCellChangedEvent>(OnPowerCellChanged);
    }

    private void OnBatteryStartup<T>(Entity<T> entity, ref ComponentStartup args) where T : BatteryAmmoProviderComponent
    {
        UpdateShots(entity, entity.Comp);
    }

    private void OnBatteryChargeChange<T>(Entity<T> entity, ref ChargeChangedEvent args) where T : BatteryAmmoProviderComponent
    {
        UpdateShots(entity, entity.Comp, args.Charge, args.MaxCharge);
    }

    private void OnPowerCellChanged<T>(Entity<T> entity, ref PowerCellChangedEvent args) where T : BatteryAmmoProviderComponent
    {
        UpdateShots(entity, entity.Comp);
    }

    private void UpdateShots(EntityUid uid, BatteryAmmoProviderComponent component)
    {
        var ev = new GetChargeEvent();
        RaiseLocalEvent(uid, ref ev);

        UpdateShots(uid, component, ev.CurrentCharge, ev.MaxCharge);
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

        if (maxShots > 0)
            component.Capacity = maxShots;

        UpdateBatteryAppearance(uid, component);

        var updateAmmoEv = new UpdateClientAmmoEvent();
        RaiseLocalEvent(uid, ref updateAmmoEv);
    }

    protected override void TakeCharge(Entity<BatteryAmmoProviderComponent> entity)
    {
        // Take charge from either the BatteryComponent or PowerCellSlotComponent.
        var ev = new ChangeChargeEvent(-entity.Comp.FireCost);
        RaiseLocalEvent(entity, ref ev);
    }
}
