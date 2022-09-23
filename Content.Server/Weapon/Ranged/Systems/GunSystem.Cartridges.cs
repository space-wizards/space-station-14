using Content.Server.Damage.Components;
using Content.Server.Projectiles.Components;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Weapon.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeCartridge()
    {
        base.InitializeCartridge();
        SubscribeLocalEvent<CartridgeAmmoComponent, ExaminedEvent>(OnCartridgeExamine);
        SubscribeLocalEvent<CartridgeAmmoComponent, GetVerbsEvent<ExamineVerb>>(OnCartridgeVerbExamine);
        SubscribeLocalEvent<CartridgeAmmoComponent, ExamineGroupEvent>(OnExamineGroup);
    }

    private void OnExamineGroup(EntityUid uid, CartridgeAmmoComponent component, ExamineGroupEvent args)
    {
        if (args.ExamineGroup != component.ExamineGroup)
            return;

        var damageSpec = GetProjectileDamage(component.Prototype);

        if (damageSpec == null)
            return;

        var type = Loc.GetString("damage-projectile");

        if (string.IsNullOrEmpty(type))
        {
            args.Entries.Add(new ExamineEntry(component.ExaminePriority, Loc.GetString("damage-examine")));
        }
        else
        {
            args.Entries.Add(new ExamineEntry(component.ExaminePriority, Loc.GetString("damage-examine-type", ("type", type))));
        }

        foreach (var damage in damageSpec.DamageDict)
        {
            if (damage.Value != FixedPoint2.Zero)
            {
                args.Entries.Add(new ExamineEntry(component.ExaminePriority-1, Loc.GetString("damage-value", ("type", damage.Key), ("amount", damage.Value))));
            }
        }

        // Get stamina damage here

        if (!ProtoManager.TryIndex<EntityPrototype>(component.Prototype, out var entityProto))
            return;
        if (entityProto.Components.TryGetValue(_factory.GetComponentName(typeof(StaminaDamageOnCollideComponent)), out var prototype))
        {
            var staminaDamageComponent = (StaminaDamageOnCollideComponent) prototype.Component;

            if (staminaDamageComponent.Damage > 0f)
            {
                args.Entries.Add(new ExamineEntry(staminaDamageComponent.ExaminePriority, Loc.GetString("damage-value", ("type", "Stamina"), ("amount", staminaDamageComponent.Damage))));
            }
        }
    }
    private void OnCartridgeVerbExamine(EntityUid uid, CartridgeAmmoComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (GetProjectileDamage(component.Prototype) == null)
            return;

        Examine.AddExamineGroupVerb(component.ExamineGroup, args);
    }

    private DamageSpecifier? GetProjectileDamage(string proto)
    {
        if (!ProtoManager.TryIndex<EntityPrototype>(proto, out var entityProto))
            return null;

        if (entityProto.Components
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

    private void OnCartridgeExamine(EntityUid uid, CartridgeAmmoComponent component, ExaminedEvent args)
    {
        if (component.Spent)
        {
            args.PushMarkup(Loc.GetString("gun-cartridge-spent"));
        }
        else
        {
            args.PushMarkup(Loc.GetString("gun-cartridge-unspent"));
        }
    }
}
