using Content.Shared._Starlight.Weapon.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.Commands.Values;
using Robust.Shared.Utility;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeCartridge()
    {
        base.InitializeCartridge();
        SubscribeLocalEvent<CartridgeAmmoComponent, ExaminedEvent>(OnCartridgeExamine);
        SubscribeLocalEvent<CartridgeAmmoComponent, DamageExamineEvent>(OnCartridgeDamageExamine);

        SubscribeLocalEvent<HitScanCartridgeAmmoComponent, ExaminedEvent>(OnHitScanCartridgeExamine);
        SubscribeLocalEvent<HitScanCartridgeAmmoComponent, DamageExamineEvent>(OnHitScanCartridgeDamageExamine);
    }


    private void OnCartridgeDamageExamine(EntityUid uid, CartridgeAmmoComponent component, ref DamageExamineEvent args)
    {
        var damageSpec = GetProjectileDamage(component.Prototype);

        if (damageSpec == null)
            return;

        _damageExamine.AddDamageExamine(args.Message, Damageable.ApplyUniversalAllModifiers(damageSpec), Loc.GetString("damage-projectile"));
    }

    private DamageSpecifier? GetProjectileDamage(string proto)
    {
        if (!ProtoManager.TryIndex<EntityPrototype>(proto, out var entityProto))
            return null;

        if (entityProto.Components
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

    private void OnHitScanCartridgeDamageExamine(EntityUid uid, HitScanCartridgeAmmoComponent component, ref DamageExamineEvent args) {
        var damageSpec = GetHitscanProjectileDamage(component.Hitscan);
        if (damageSpec == null)
            return;

        _damageExamine.AddDamageExamine(args.Message, Damageable.ApplyUniversalAllModifiers(damageSpec), Loc.GetString("damage-projectile"));
        
        var ArmorMessage = GetArmorPenetrationExplain(component.Hitscan);

        args.Message.AddMessage(ArmorMessage);

    }

    private FormattedMessage GetArmorPenetrationExplain(string proto) {
        var msg = new FormattedMessage();
        if (!ProtoManager.TryIndex<HitscanPrototype>(proto,out var entityProto))
            return msg;
        
        if (entityProto.ArmorPenetration == 0) {
            return msg;
        }
        if (entityProto.ArmorPenetration > 0){
            msg.PushNewline();
            msg.TryAddMarkup(Loc.GetString("damage-examine-penetration-positive",("penetration",entityProto.ArmorPenetration*100)),out var error);
        }
        if(entityProto.ArmorPenetration < 0) {
            msg.PushNewline();
            msg.TryAddMarkup(Loc.GetString("damage-examine-penetration-negative",("penetration",entityProto.ArmorPenetration*-100)),out var error);
        }
        return msg;
    }

    private DamageSpecifier? GetHitscanProjectileDamage(string proto) {
        if (!ProtoManager.TryIndex<HitscanPrototype>(proto,out var entityProto))
            return null;
        
        if (entityProto.Damage == null) {
            return null;
        }

        if (!entityProto.Damage.Empty) {
            return entityProto.Damage * Damageable.UniversalHitscanDamageModifier;
        }

        return null;
    }

        private void OnHitScanCartridgeExamine(EntityUid uid, HitScanCartridgeAmmoComponent component, ExaminedEvent args)
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