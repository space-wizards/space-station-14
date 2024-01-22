using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Shared.Execution;

/// <summary>
///     Verb for violently murdering cuffed creatures.
/// </summary>
public sealed class ExecutionSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

    private const string DefaultInternalMeleeSuicideMessage = "suicide-popup-melee-initial-internal";
    private const string DefaultExternalMeleeSuicideMessage = "suicide-popup-melee-initial-external";
    private const string DefaultInternalMeleeExecutionMessage = "execution-popup-melee-initial-internal";
    private const string DefaultExternalMeleeExecutionMessage = "execution-popup-melee-initial-external";
    private const string DefaultCompleteInternalMeleeSuicideMessage = "suicide-popup-melee-complete-internal";
    private const string DefaultCompleteExternalMeleeSuicideMessage = "suicide-popup-melee-complete-external";
    private const string DefaultCompleteInternalMeleeExecutionMessage = "execution-popup-melee-complete-internal";
    private const string DefaultCompleteExternalMeleeExecutionMessage = "execution-popup-melee-complete-external";
    private const string DefaultInternalGunSuicideMessage = "suicide-popup-gun-initial-internal";
    private const string DefaultExternalGunSuicideMessage = "suicide-popup-gun-initial-external";
    private const string DefaultInternalGunExecutionMessage = "execution-popup-gun-initial-internal";
    private const string DefaultExternalGunExecutionMessage = "execution-popup-gun-initial-external";
    private const string DefaultCompleteInternalGunSuicideMessage = "suicide-popup-gun-complete-internal";
    private const string DefaultCompleteExternalGunSuicideMessage = "suicide-popup-gun-complete-external";
    private const string DefaultCompleteInternalGunExecutionMessage = "execution-popup-gun-complete-internal";
    private const string DefaultCompleteExternalGunExecutionMessage = "execution-popup-gun-complete-external";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExecutionComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionsVerbs);

        SubscribeLocalEvent<MeleeWeaponComponent, ExecutionDoAfterEvent>(OnDoafterMelee);
        SubscribeLocalEvent<GunComponent, ExecutionDoAfterEvent>(OnDoafterGun);

        SubscribeLocalEvent<ActiveExecutionComponent, AmmoShotEvent>(OnAmmoShot);

        SubscribeLocalEvent<ExecutionProjectileComponent, StartCollideEvent>(OnCollide);
    }

    private void OnGetInteractionsVerbs(EntityUid uid, ExecutionComponent comp, GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return;

        var attacker = args.User;
        var weapon = args.Using.Value;
        var victim = args.Target;

        if (HasComp<MeleeWeaponComponent>(weapon) && !CanExecuteWithMelee(weapon, victim, attacker))
            return;

        if (HasComp<GunComponent>(weapon) && !CanExecuteWithGun(weapon, victim, attacker))
            return;

        UtilityVerb verb = new()
        {
            Act = () => TryStartExecutionDoAfter(weapon, victim, attacker, comp),
            Impact = LogImpact.High,
            Text = Loc.GetString("execution-verb-name"),
            Message = Loc.GetString("execution-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private void TryStartExecutionDoAfter(EntityUid weapon, EntityUid victim, EntityUid attacker, ExecutionComponent comp)
    {
        if (HasComp<MeleeWeaponComponent>(weapon) && !CanExecuteWithMelee(weapon, victim, attacker))
            return;

        var defaultSuicideInternal = DefaultInternalMeleeSuicideMessage;
        var defaultSuicideExternal = DefaultExternalMeleeSuicideMessage;
        var defaultExecutionInternal = DefaultInternalMeleeExecutionMessage;
        var defaultExecutionExternal = DefaultExternalMeleeExecutionMessage;

        if (HasComp<GunComponent>(weapon))
        {
            if(!CanExecuteWithGun(weapon, victim, attacker))
                return;

            defaultSuicideExternal = DefaultExternalGunSuicideMessage;
            defaultSuicideInternal = DefaultInternalGunSuicideMessage;
            defaultExecutionExternal = DefaultInternalGunExecutionMessage;
            defaultExecutionInternal = DefaultExternalGunExecutionMessage;
        }


        string internalMsg;
        string externalMsg;

        if (attacker == victim)
        {
            internalMsg = comp.SuicidePopupInternal ?? defaultSuicideInternal;
            externalMsg = comp.SuicidePopupExternal ?? defaultSuicideExternal;
        }
        else
        {
            internalMsg = comp.ExecutionPopupInternal ?? defaultExecutionInternal;
            externalMsg = comp.ExecutionPopupExternal ?? defaultExecutionExternal;
        }
        ShowExecutionInternalPopup(internalMsg, attacker, victim, weapon);
        ShowExecutionExternalPopup(externalMsg, attacker, victim, weapon);

        var doAfter =
            new DoAfterArgs(EntityManager, attacker, comp.DoAfterDuration, new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        _doAfterSystem.TryStartDoAfter(doAfter);

    }

    private bool CanExecuteWithAny(EntityUid victim, EntityUid attacker)
    {
        // No point executing someone if they can't take damage
        if (!TryComp<DamageableComponent>(victim, out _))
            return false;

        // You can't execute something that cannot die
        if (!TryComp<MobStateComponent>(victim, out var mobState))
            return false;

        // You're not allowed to execute dead people (no fun allowed)
        if (_mobStateSystem.IsDead(victim, mobState))
            return false;

        // You must be able to attack people to execute
        if (!_actionBlockerSystem.CanAttack(attacker, victim))
            return false;

        // The victim must be incapacitated to be executed
        if (victim != attacker && _actionBlockerSystem.CanInteract(victim, null))
            return false;

        // All checks passed
        return true;
    }

    private bool CanExecuteWithMelee(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!CanExecuteWithAny(victim, user))
            return false;

        // We must be able to actually hurt people with the weapon
        if (!TryComp<MeleeWeaponComponent>(weapon, out var melee) || melee.Damage.GetTotal() <= 0.0f)
            return false;

        return true;
    }

    private bool CanExecuteWithGun(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!CanExecuteWithAny(victim, user))
            return false;

        // We must be able to actually fire the gun
        if (!TryComp<GunComponent>(weapon, out var gun) || _gunSystem.CanShoot(gun))
            return false;

        return true;
    }


    private void OnDoafterMelee(EntityUid uid, MeleeWeaponComponent comp, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;

        if (!TryComp<ExecutionComponent>(uid, out var executionComp))
            return;

        var attacker = args.User;
        var victim = args.Target.Value;
        var weapon = args.Used.Value;

        if (!CanExecuteWithMelee(weapon, victim, attacker))
            return;

        _damageableSystem.TryChangeDamage(victim, comp.Damage * executionComp.DamageModifier, true);

        _audioSystem.PlayPredicted(comp.HitSound, weapon, attacker);

        string internalMsg;
        string externalMsg;

        if (attacker == victim)
        {
            internalMsg = executionComp.SuicidePopupCompleteInternal ?? DefaultCompleteInternalMeleeSuicideMessage;
            externalMsg = executionComp.SuicidePopupCompleteExternal ?? DefaultCompleteExternalMeleeSuicideMessage;
        }
        else
        {
            internalMsg = executionComp.ExecutionPopupCompleteInternal ?? DefaultCompleteInternalMeleeExecutionMessage;
            externalMsg = executionComp.ExecutionPopupCompleteExternal ?? DefaultCompleteExternalMeleeExecutionMessage;
        }
        ShowExecutionInternalPopup(internalMsg, attacker, victim, weapon);
        ShowExecutionExternalPopup(externalMsg, attacker, victim, weapon);

        args.Handled = true;
    }

    private void OnDoafterGun(EntityUid uid, GunComponent comp, DoAfterEvent args)
    {
        if (!TryComp<ExecutionComponent>(uid, out var executionComponent))
            return;

        if (!TryComp<TransformComponent>(args.Target, out var xform))
            return;

        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;

        var attacker = args.User;
        var weapon = args.Used.Value;
        var victim = args.Target.Value;
        var clumsyShot = false;
        // Clumsy people have a chance to shoot themselves
        if (TryComp<ClumsyComponent>(attacker, out var clumsy) && comp.ClumsyProof == false)
        {
            if (_interactionSystem.TryRollClumsy(attacker, 0.333f, clumsy))
            {
                clumsyShot = true;
            }
        }

        if (!CanExecuteWithGun(weapon, victim, attacker))
            return;

        var active = EnsureComp<ActiveExecutionComponent>(uid);
        active.Attacker = attacker;
        active.Victim = victim;
        active.Clumsy = clumsyShot;
        active.FixtureId = executionComponent.FixtureId;
        Dirty(uid, active);

        EntityCoordinates coords;
        if (victim == attacker || clumsyShot)
        {
            // Should I be creating EntityCoordinates out of thin air? Probably not but this is the best way I can think
            // of to actually fire a projectile where the start and end positions aren't the same.
            coords = new EntityCoordinates(EntityUid.Invalid, xform.Coordinates.X + 1, xform.Coordinates.Y);
        }
        else
        {
            coords = xform.Coordinates;
        }

        _gunSystem.AttemptShoot(args.User, uid, comp, coords);

        RemCompDeferred<ActiveExecutionComponent>(uid);
        Dirty(uid, active);

        args.Handled = true;
    }

    private void OnAmmoShot(EntityUid uid, ActiveExecutionComponent comp, AmmoShotEvent args)
    {
        if (!TryComp<ExecutionComponent>(uid, out var executionComp))
            return;

        if (args.FiredProjectiles.Count < 1)
            return;

        foreach (var bullet in args.FiredProjectiles)
        {

            if (comp.Attacker == comp.Victim || comp.Clumsy)
            {
                if (!TryComp<ProjectileComponent>(bullet, out var projComponent))
                    return;

                projComponent.IgnoreShooter = false;

                Dirty(bullet, projComponent);
            }

            if (!comp.Clumsy)
            {
                EnsureComp<ExecutionProjectileComponent>(bullet, out var execBulletComp);

                execBulletComp.Target = comp.Victim;
                execBulletComp.Multiplier = executionComp.DamageModifier;
                execBulletComp.Clumsy = comp.Clumsy;
                execBulletComp.FixtureId = comp.FixtureId;

                Dirty(bullet, execBulletComp);
            }
        }


        string internalMsg;
        string externalMsg;

        var attacker = comp.Attacker;
        var victim = comp.Victim;

        if (comp.Clumsy)
        {
            internalMsg = "execution-popup-gun-clumsy-internal";
            externalMsg = "execution-popup-gun-clumsy-external";
        }
        else if (attacker == victim)
        {
            internalMsg = executionComp.SuicidePopupCompleteInternal ?? DefaultCompleteInternalGunSuicideMessage;
            externalMsg = executionComp.SuicidePopupCompleteExternal ?? DefaultCompleteExternalGunSuicideMessage;
        }
        else
        {
            internalMsg = executionComp.ExecutionPopupCompleteInternal ?? DefaultCompleteInternalGunExecutionMessage;
            externalMsg = executionComp.ExecutionPopupCompleteExternal ?? DefaultCompleteExternalGunExecutionMessage;
        }

        ShowExecutionInternalPopup(internalMsg, attacker, victim, uid, false);
        ShowExecutionExternalPopup(externalMsg, attacker, victim, uid);
    }

    private void OnCollide(EntityUid uid, ExecutionProjectileComponent comp, StartCollideEvent args)
    {
        if (!(args.OtherEntity == comp.Target) || args.OurFixtureId != comp.FixtureId)
            return;

        if (!TryComp<ProjectileComponent>(uid, out var projectileComponent))
            return;

        projectileComponent.Damage *= comp.Multiplier;
    }

    private void ShowExecutionInternalPopup(string locString,
        EntityUid attacker, EntityUid victim, EntityUid weapon, bool predict = true)
    {
        if (predict)
        {
            _popupSystem.PopupClient(
                Loc.GetString(locString, ("attacker", attacker), ("victim", victim), ("weapon", weapon)),
                attacker,
                attacker,
                PopupType.Medium
            );
        }
        else
        {
            _popupSystem.PopupEntity(
                Loc.GetString(locString, ("attacker", attacker), ("victim", victim), ("weapon", weapon)),
                attacker,
                Filter.Entities(attacker),
                true,
                PopupType.Medium
            );
        }

    }

    private void ShowExecutionExternalPopup(string locString, EntityUid attacker, EntityUid victim, EntityUid weapon)
    {
        _popupSystem.PopupEntity(
            Loc.GetString(locString, ("attacker", attacker), ("victim", victim), ("weapon", weapon)),
            attacker,
            Filter.PvsExcept(attacker),
            true,
            PopupType.MediumCaution
            );
    }

}
