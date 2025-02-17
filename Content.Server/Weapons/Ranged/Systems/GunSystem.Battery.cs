using Content.Server.Power.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.FixedPoint;
using Content.Shared.PowerCell.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
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
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, DamageExamineEvent>(OnBatteryDamageExamine);
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, PowerCellChangedEvent>(OnPowerCellChanged);

        // Projectile
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, DamageExamineEvent>(OnBatteryDamageExamine);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, PowerCellChangedEvent>(OnPowerCellChanged);
    }

    private void OnBatteryStartup(EntityUid uid, BatteryAmmoProviderComponent component, ComponentStartup args)
    {
        UpdateShots(uid, component);
    }

    private void OnBatteryChargeChange(EntityUid uid, BatteryAmmoProviderComponent component, ref ChargeChangedEvent args)
    {
        UpdateShots(uid, component, args.Charge, args.MaxCharge);
    }

    private void OnPowerCellChanged(EntityUid uid, BatteryAmmoProviderComponent component, ref PowerCellChangedEvent args)
    {
        UpdateShots(uid, component);
    }

    private void UpdateShots(Entity<BatteryAmmoProviderComponent> entity)
    {
        UpdateShots(entity.Owner, entity.Comp);
    }

    private void UpdateShots(EntityUid uid, BatteryAmmoProviderComponent component)
    {
        var currentCharge = 0f;
        var maxCharge = 0f;
        if (TryComp<BatteryComponent>(uid, out var battery))
        {
            currentCharge = battery.CurrentCharge;
            maxCharge = battery.MaxCharge;
        }
        else if (TryComp<PowerCellSlotComponent>(uid, out var powerCellSlot) &&
            _powerCell.TryGetBatteryFromSlot(uid, out var powerCell, powerCellSlot))
        {
            currentCharge = powerCell.CurrentCharge;
            maxCharge = powerCell.MaxCharge;
        }

        UpdateShots(uid, component, currentCharge, maxCharge);
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

    private void OnBatteryDamageExamine(EntityUid uid, BatteryAmmoProviderComponent component, ref DamageExamineEvent args)
    {
        var damageSpec = GetDamage(component);

        if (damageSpec == null)
            return;

        var damageType = component switch
        {
            HitscanBatteryAmmoProviderComponent => Loc.GetString("damage-hitscan"),
            ProjectileBatteryAmmoProviderComponent => Loc.GetString("damage-projectile"),
            _ => throw new ArgumentOutOfRangeException(),
        };

        _damageExamine.AddDamageExamine(args.Message, damageSpec, damageType);
    }

    private DamageSpecifier? GetDamage(BatteryAmmoProviderComponent component)
    {
        if (component is ProjectileBatteryAmmoProviderComponent battery)
        {
            if (ProtoManager.Index<EntityPrototype>(battery.Prototype).Components
                .TryGetValue(_factory.GetComponentName(typeof(ProjectileComponent)), out var projectile))
            {
                var p = (ProjectileComponent) projectile.Component;

                if (!p.Damage.Empty)
                {
                    return p.Damage;
                }
            }

            return null;
        }

        if (component is HitscanBatteryAmmoProviderComponent hitscan)
        {
            return ProtoManager.Index<HitscanPrototype>(hitscan.Prototype).Damage;
        }

        return null;
    }

    protected override void TakeCharge(Entity<BatteryAmmoProviderComponent> entity)
    {
        if (TryComp<BatteryComponent>(entity, out var battery))
            _battery.UseCharge(entity, entity.Comp.FireCost, battery);
        else if (TryComp<PowerCellSlotComponent>(entity, out var powerCellSlot))
            _powerCell.TryUseCharge(entity, entity.Comp.FireCost, powerCellSlot);
    }
}
