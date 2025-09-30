using Content.Server.Kitchen.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Chat;
using Content.Shared.Clumsy;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Execution;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared._Starlight.Weapon.Components;
using Content.Shared.Kitchen.Components;

namespace Content.Server.Execution;

/// <summary>
///     Verb for violently murdering cuffed creatures.
/// </summary>
public sealed class ExecutionSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSuicideSystem _suicide = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharpComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionVerbsMelee);
        SubscribeLocalEvent<SharpComponent, ExecutionDoAfterEvent>(OnExecutionDoAfterMelee);

        SubscribeLocalEvent<GunComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionVerbsGun);
        SubscribeLocalEvent<GunComponent, ExecutionDoAfterEvent>(OnExecutionDoAfterGun);
    }

    private void OnGetInteractionVerbsMelee(EntityUid uid, SharpComponent comp, GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return;

        var attacker = args.User;
        var weapon = args.Using!.Value;
        var victim = args.Target;

        if (!CanBeExecutedWithMelee(weapon, victim, attacker)
            || !CanBeExecutedWithAny(victim, attacker))
            return;

        UtilityVerb verb = new()
        {
            Act = () => TryStartExecutionDoAfter(weapon, victim, attacker),
            Impact = LogImpact.High,
            Text = attacker == victim ? Loc.GetString("suicide-verb-name") : Loc.GetString("execution-verb-name"),
            Message = attacker == victim ? Loc.GetString("suicide-verb-message") : Loc.GetString("execution-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private void OnGetInteractionVerbsGun(EntityUid uid, GunComponent comp, GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return;

        var attacker = args.User;
        var weapon = args.Using!.Value;
        var victim = args.Target;

        if (!CanBeExecutedWithGun(weapon, victim, attacker)
            || !CanBeExecutedWithAny(victim, attacker))
            return;

        UtilityVerb verb = new()
        {
            Act = () =>
            {
                TryStartGunExecutionDoafter(weapon, victim, attacker);
            },
            Impact = LogImpact.High,
            Text = attacker == victim ? Loc.GetString("suicide-verb-name") : Loc.GetString("execution-verb-name"),
            Message = attacker == victim ? Loc.GetString("suicide-verb-message") : Loc.GetString("execution-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private void TryStartExecutionDoAfter(EntityUid weapon, EntityUid victim, EntityUid attacker)
    {
        if (!CanBeExecutedWithMelee(weapon, victim, attacker))
            return;

        if (attacker == victim)
        {
            ShowExecutionInternalPopup(ExecutionComponent.InternalSelfMeleeExecutionMessage, attacker, victim, weapon);
            ShowExecutionExternalPopup(ExecutionComponent.ExternalSelfMeleeExecutionMessage, attacker, victim, weapon);
        }
        else
        {
            ShowExecutionInternalPopup(ExecutionComponent.ExternalSelfMeleeExecutionMessage, attacker, victim, weapon);
            ShowExecutionExternalPopup(ExecutionComponent.ExternalMeleeExecutionMessage, attacker, victim, weapon);
        }

        var doAfter =
            new DoAfterArgs(EntityManager, attacker, ExecutionComponent.MeleeDoAfterDuration, new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        _doAfter.TryStartDoAfter(doAfter);

    }

    private void TryStartGunExecutionDoafter(EntityUid weapon, EntityUid victim, EntityUid attacker)
    {
        if (!CanBeExecutedWithGun(weapon, victim, attacker))
            return;

        if (!TryComp<GunComponent>(weapon, out var gunComponent))
            return;

        var shotAttempted = new ShotAttemptedEvent
        {
            User = attacker,
            Used = (weapon, gunComponent),
        };
        RaiseLocalEvent(weapon, ref shotAttempted);
        if (shotAttempted.Cancelled)
        {
            if (shotAttempted.Message != null)
                _popup.PopupEntity(shotAttempted.Message, weapon, attacker);
            return;
        }

        if (attacker == victim)
        {
            ShowExecutionInternalPopup(ExecutionComponent.InternalSelfGunExecutionMessage, attacker, victim, weapon);
            ShowExecutionExternalPopup(ExecutionComponent.ExternalSelfGunExecutionMessage, attacker, victim, weapon);
        }
        else
        {
            ShowExecutionInternalPopup(ExecutionComponent.InternalGunExecutionMessage, attacker, victim, weapon);
            ShowExecutionExternalPopup(ExecutionComponent.ExternalGunExecutionMessage, attacker, victim, weapon);
        }

        var doAfter =
            new DoAfterArgs(EntityManager, attacker, ExecutionComponent.GunDoAfterDuration, new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        _doAfter.TryStartDoAfter(doAfter);
    }

    public bool CanBeExecutedWithAny(EntityUid victim, EntityUid attacker)
    {
        // No point executing someone if they can't take damage
        if (!HasComp<DamageableComponent>(victim))
            return false;

        // You can't execute something that cannot die
        if (!TryComp<MobStateComponent>(victim, out var mobState))
            return false;

        // You're not allowed to execute dead people (no fun allowed)
        if (_mobState.IsDead(victim, mobState))
            return false;

        // You must be able to attack people to execute
        if (!_actionBlocker.CanAttack(attacker, victim))
            return false;

        // The victim must be incapacitated to be executed
        if (victim != attacker && _actionBlocker.CanInteract(victim, null))
            return false;

        // All checks passed
        return true;
    }

    private bool CanBeExecutedWithMelee(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!CanBeExecutedWithAny(victim, user))
            return false;

        // We must be able to actually hurt people with the weapon
        if (!TryComp<MeleeWeaponComponent>(weapon, out var melee) && melee!.Damage.GetTotal() > 0.0f)
            return false;

        return true;
    }

    private bool CanBeExecutedWithGun(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!CanBeExecutedWithAny(victim, user))
            return false;

        // We must be able to actually fire the gun
        if (!TryComp<GunComponent>(weapon, out var gun) && _gunSystem.CanShoot(gun!))
            return false;

        if (_appearanceSystem.TryGetData(weapon, AmmoVisuals.BoltClosed, out bool boltClosed))
            if (!boltClosed)
                return false;

        return true;
    }

    private void ShowExecutionInternalPopup(string locString, EntityUid attacker, EntityUid victim, EntityUid weapon, bool predict = true)
    {
        if (predict)
        {
            _popup.PopupEntity(
               Loc.GetString(locString, ("attacker", Identity.Entity(attacker, EntityManager)), ("victim", Identity.Entity(victim, EntityManager)), ("weapon", weapon)), attacker, Filter.Entities(attacker), true, PopupType.MediumCaution);
        }
        else
        {
            _popup.PopupEntity(
               Loc.GetString(locString, ("attacker", Identity.Entity(attacker, EntityManager)), ("victim", Identity.Entity(victim, EntityManager)), ("weapon", weapon)), attacker, Filter.Entities(attacker), true, PopupType.MediumCaution);
        }
    }

    private void ShowExecutionExternalPopup(string locString, EntityUid attacker, EntityUid victim, EntityUid weapon)
    {
        _popup.PopupEntity(
            Loc.GetString(locString, ("attacker", Identity.Entity(attacker, EntityManager)), ("victim", Identity.Entity(victim, EntityManager)), ("weapon", weapon)), attacker, Filter.PvsExcept(attacker), true, PopupType.MediumCaution);
    }

    private void OnExecutionDoAfterMelee(EntityUid entity, SharpComponent component, ref ExecutionDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;

        if (!TryComp<MeleeWeaponComponent>(entity, out var meleeWeaponComp))
            return;

        var attacker = args.User;
        var victim = args.Target.Value;
        var weapon = args.Used.Value;

        if (!CanBeExecutedWithMelee(weapon, victim, attacker))
            return;

        // This is needed so the melee system does not stop it.
        var prev = _combat.IsInCombatMode(attacker);
        _combat.SetInCombatMode(attacker, true);

        var internalMsg = ExecutionComponent.CompleteInternalMeleeExecutionMessage;
        var externalMsg = ExecutionComponent.CompleteExternalMeleeExecutionMessage;

        if (attacker == victim)
        {

            if (!TryComp<DamageableComponent>(victim, out var damageableComponent))
                return;

            _audio.PlayPredicted(meleeWeaponComp.HitSound, victim, victim);
            _suicide.ApplyLethalDamage((victim, damageableComponent), meleeWeaponComp.Damage);
        }
        else
        {
            _damageableSystem.TryChangeDamage(victim, meleeWeaponComp.Damage * ExecutionComponent.DamageMultiplier, true);
        }

        _combat.SetInCombatMode(attacker, prev);
        args.Handled = true;

        if (attacker != victim)
        {
            ShowExecutionInternalPopup(internalMsg, attacker, victim, entity);
            ShowExecutionExternalPopup(externalMsg, attacker, victim, entity);
        }
        else
        {
            ShowExecutionInternalPopup(ExecutionComponent.CompleteInternalSelfMeleeExecutionMessage, victim, victim, entity, false);
            ShowExecutionExternalPopup(ExecutionComponent.CompleteExternalSelfMeleeExecutionMessage, victim, victim, entity);
        }
    }

    private void OnExecutionDoAfterGun(EntityUid uid, GunComponent component, ref ExecutionDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;

        var attacker = args.User;
        var weapon = args.Used.Value;
        var victim = args.Target.Value;

        if (!CanBeExecutedWithGun(weapon, victim, attacker))
            return;

        // Check if any systems want to block our shot
        var prevention = new ShotAttemptedEvent
        {
            User = attacker,
            Used = (weapon, component),
        };

        RaiseLocalEvent(weapon, ref prevention);
        if (prevention.Cancelled)
            return;

        RaiseLocalEvent(attacker, ref prevention);
        if (prevention.Cancelled)
            return;

        // Not sure what this is for but gunsystem uses it so ehhh
        var attemptEv = new AttemptShootEvent(attacker, null);
        RaiseLocalEvent(weapon, ref attemptEv);

        if (attemptEv.Cancelled)
        {
            if (attemptEv.Message != null)
                _popup.PopupClient(attemptEv.Message, weapon, attacker);
            return;
        }

        // Take some ammunition for the shot (one bullet)
        var fromCoordinates = Transform(attacker).Coordinates;
        var ev = new TakeAmmoEvent(1, new List<(EntityUid? Entity, IShootable Shootable)>(), fromCoordinates, attacker);
        RaiseLocalEvent(weapon, ev);

        // Check if there's any ammo left
        if (ev.Ammo.Count <= 0)
        {
            _audio.PlayEntity(component.SoundEmpty, Filter.Pvs(weapon), weapon, true, AudioParams.Default);
            ShowExecutionExternalPopup("execution-popup-gun-empty", attacker, victim, weapon);
            return;
        }

        // Information about the ammo like damage
        DamageSpecifier damage = new DamageSpecifier();

        // Get some information from IShootable
        var ammoUid = ev.Ammo[0].Entity;
        switch (ev.Ammo[0].Shootable)
        {
            //🌟Starlight🌟 start
            case HitScanCartridgeAmmoComponent cartridge:
                var hitscanProto = _prototypeManager.Index(cartridge.Hitscan);
                if (hitscanProto.Damage is not null)
                    damage = hitscanProto.Damage * hitscanProto.Count;

                cartridge.Spent = true;
                _appearanceSystem.SetData(ammoUid!.Value, AmmoVisuals.Spent, true);
                Dirty(ammoUid.Value, cartridge);

                break;
            //🌟Starlight🌟 end
            case CartridgeAmmoComponent cartridge:
                // Get the damage value
                var prototype = _prototypeManager.Index<EntityPrototype>(cartridge.Prototype);
                prototype.TryGetComponent<ProjectileComponent>(out var projectileA, _componentFactory); // sloth forgive me
                if (projectileA != null)
                {
                    damage = projectileA.Damage;
                }
                prototype.TryGetComponent<ProjectileSpreadComponent>(out var projectilespreaderA, _componentFactory);
                if (projectilespreaderA != null)
                {
                    damage *= projectilespreaderA.Count;
                }

                // Expend the cartridge
                cartridge.Spent = true;
                _appearanceSystem.SetData(ammoUid!.Value, AmmoVisuals.Spent, true);
                Dirty(ammoUid.Value, cartridge);

                break;

            case AmmoComponent newAmmo:
                TryComp<ProjectileComponent>(ammoUid, out var projectileB);
                if (projectileB != null)
                {
                    damage = projectileB.Damage;
                }
                Del(ammoUid);
                break;

            case HitscanPrototype hitscan:
                damage = hitscan.Damage!;
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        // Clumsy people have a chance to shoot themselves
        if (TryComp<ClumsyComponent>(attacker, out var clumsy) && component.ClumsyProof == false)
        {
            if (_random.Prob(0.33333333f))
            {
                ShowExecutionInternalPopup("execution-popup-gun-clumsy-internal", attacker, victim, weapon);
                ShowExecutionExternalPopup("execution-popup-gun-clumsy-external", attacker, victim, weapon);

                // You shoot yourself with the gun (no damage multiplier)
                _damageableSystem.TryChangeDamage(attacker, damage, origin: attacker);
                _audio.PlayEntity(component.SoundGunshot, Filter.Pvs(weapon), weapon, true, AudioParams.Default);
                return;
            }
        }

        // Gun successfully fired, deal damage
        _damageableSystem.TryChangeDamage(victim, damage * ExecutionComponent.DamageMultiplier, true);
        _audio.PlayEntity(component.SoundGunshot, Filter.Pvs(weapon), weapon, false, AudioParams.Default);

        // Popups
        if (attacker != victim)
        {
            ShowExecutionInternalPopup(ExecutionComponent.CompleteInternalGunExecutionMessage, attacker, victim, weapon);
            ShowExecutionExternalPopup(ExecutionComponent.CompleteExternalGunExecutionMessage, attacker, victim, weapon);
        }
        else
        {
            ShowExecutionInternalPopup(ExecutionComponent.CompleteInternalSelfGunExecutionMessage, attacker, victim, weapon, false);
            ShowExecutionExternalPopup(ExecutionComponent.CompleteExternalSelfGunExecutionMessage, attacker, victim, weapon);
        }
    }
}
