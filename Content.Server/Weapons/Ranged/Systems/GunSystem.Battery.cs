using Content.Server.Power.Components;
using Content.Server.Projectiles.Components;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Verbs;
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
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, GetVerbsEvent<ExamineVerb>>(OnBatteryExaminableVerb);
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, ExamineGroupEvent>(OnExamineGroup);

        // Projectile
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, GetVerbsEvent<ExamineVerb>>(OnBatteryExaminableVerb);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, ExamineGroupEvent>(OnExamineGroup);

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

        if (component.Shots != shots || component.Capacity != maxShots)
        {
            Dirty(component);
        }

        component.Shots = shots;
        component.Capacity = maxShots;
        UpdateBatteryAppearance(component.Owner, component);
    }

    private void OnExamineGroup(EntityUid uid, BatteryAmmoProviderComponent component, ExamineGroupEvent args)
    {
        if (args.ExamineGroup != component.ExamineGroup)
            return;

        var damageSpec = GetDamage(component);

        if (damageSpec == null)
            return;

        string damageType;

        switch (component)
        {
            case HitscanBatteryAmmoProviderComponent:
                damageType = Loc.GetString("damage-hitscan");
                break;
            case ProjectileBatteryAmmoProviderComponent:
                damageType = Loc.GetString("damage-projectile");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (string.IsNullOrEmpty(damageType))
        {
            args.Entries.Add(new ExamineEntry(component.ExaminePriority, Loc.GetString("damage-examine")));
        }
        else
        {
            args.Entries.Add(new ExamineEntry(component.ExaminePriority, Loc.GetString("damage-examine-type", ("type", damageType))));
        }

        foreach (var damage in damageSpec.DamageDict)
        {
            if (damage.Value != FixedPoint2.Zero)
            {
                args.Entries.Add(new ExamineEntry(component.ExaminePriority-1, Loc.GetString("damage-value", ("type", damage.Key), ("amount", damage.Value))));
            }
        }

    }

    private void OnBatteryExaminableVerb(EntityUid uid, BatteryAmmoProviderComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (GetDamage(component) == null)
            return;

        Examine.AddExamineGroupVerb(component.ExamineGroup, args);
    }

    private DamageSpecifier? GetDamage(BatteryAmmoProviderComponent component)
    {
        if (component is ProjectileBatteryAmmoProviderComponent battery)
        {
            if (ProtoManager.Index<EntityPrototype>(battery.Prototype).Components
                .TryGetValue(_factory.GetComponentName(typeof(ProjectileComponent)), out var projectile))
            {
                var p = (ProjectileComponent) projectile.Component;

                if (p.Damage.Total > FixedPoint2.Zero)
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

    protected override void TakeCharge(EntityUid uid, BatteryAmmoProviderComponent component)
    {
        if (!TryComp<BatteryComponent>(uid, out var battery)) return;

        battery.CurrentCharge -= component.FireCost;
        UpdateShots(component, battery);
    }
}
