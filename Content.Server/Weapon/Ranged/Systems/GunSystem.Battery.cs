using Content.Server.Power.Components;
using Content.Server.Projectiles.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Weapon.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeBattery()
    {
        base.InitializeBattery();

        // Hitscan
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, GetVerbsEvent<ExamineVerb>>(OnBatteryExaminableVerb);

        // Projectile
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, GetVerbsEvent<ExamineVerb>>(OnBatteryExaminableVerb);
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

    private void OnBatteryExaminableVerb(EntityUid uid, BatteryAmmoProviderComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var damageSpec = GetDamage(component);

        if (damageSpec == null)
            return;

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = GetBatteryMarkup(component);
                Examine.SendExamineTooltip(args.User, uid, markup, false, false);
            },
            Text = Loc.GetString("damage-examinable-verb-text"),
            Message = Loc.GetString("damage-examinable-verb-message"),
            Category = VerbCategory.Examine,
            IconTexture = "/Textures/Interface/VerbIcons/smite.svg.192dpi.png"
        };

        args.Verbs.Add(verb);
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

    private FormattedMessage GetBatteryMarkup(BatteryAmmoProviderComponent component)
    {
        var damageSpec = GetDamage(component);

        if (damageSpec == null)
            return new FormattedMessage();

        var msg = new FormattedMessage();
        msg.AddMarkup(Loc.GetString("damage-examine"));

        foreach (var damage in damageSpec.DamageDict)
        {
            msg.PushNewline();
            msg.AddMarkup(Loc.GetString("damage-value", ("type", damage.Key), ("amount", damage.Value)));
        }

        return msg;
    }

    protected override void TakeCharge(EntityUid uid, BatteryAmmoProviderComponent component)
    {
        if (!TryComp<BatteryComponent>(uid, out var battery)) return;

        battery.CurrentCharge -= component.FireCost;
        UpdateShots(component, battery);
    }
}
