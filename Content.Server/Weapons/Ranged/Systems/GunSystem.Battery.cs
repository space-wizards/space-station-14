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

    private void OnBatteryDamageExamine<T>(Entity<T> entity, ref DamageExamineEvent args) where T : BatteryAmmoProviderComponent
    {
        var damageSpec = GetDamage(entity.Comp);

        if (damageSpec == null)
            return;

        var damageType = entity.Comp switch
        {
            HitscanBatteryAmmoProviderComponent => Loc.GetString("damage-hitscan"),
            ProjectileBatteryAmmoProviderComponent => Loc.GetString("damage-projectile"),
            _ => throw new ArgumentOutOfRangeException(),
        };

        _damageExamine.AddDamageExamine(args.Message, Damageable.ApplyUniversalAllModifiers(damageSpec), damageType);
    }

    private DamageSpecifier? GetDamage(BatteryAmmoProviderComponent component)
    {
        if (component is ProjectileBatteryAmmoProviderComponent battery)
        {
            if (ProtoManager.Index<EntityPrototype>(battery.Prototype).Components
                .TryGetValue(Factory.GetComponentName<ProjectileComponent>(), out var projectile))
            {
                var p = (ProjectileComponent) projectile.Component;

                if (!p.Damage.Empty)
                {
                    return p.Damage * Damageable.UniversalProjectileDamageModifier;
                }
            }

            return null;
        }

        if (component is HitscanBatteryAmmoProviderComponent hitscan)
        {
            var dmg = ProtoManager.Index<HitscanPrototype>(hitscan.Prototype).Damage;
            return dmg == null ? dmg : dmg * Damageable.UniversalHitscanDamageModifier;
        }

        return null;
    }

    protected override void TakeCharge(Entity<BatteryAmmoProviderComponent> entity)
    {
        var ev = new ChangeChargeEvent(-entity.Comp.FireCost);
        RaiseLocalEvent(entity, ref ev);
    }
}
