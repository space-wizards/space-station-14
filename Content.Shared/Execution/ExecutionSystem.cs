using Content.Shared.Interaction;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
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

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExecutionComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionsVerbs);

        SubscribeLocalEvent<MeleeWeaponComponent, ExecutionDoAfterEvent>(OnDoafterMelee);
        SubscribeLocalEvent<GunComponent, ExecutionDoAfterEvent>(OnDoafterGun);

        SubscribeLocalEvent<ActiveExecutionComponent, AmmoShotEvent>(OnAmmoShot);
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

        if (HasComp<GunComponent>(weapon) && !CanExecuteWithGun(weapon, victim, attacker))
            return;

        string internalMsg;
        string externalMsg;

        if (attacker == victim)
        {
            internalMsg = comp.SuicidePopupInternal;
            externalMsg = comp.SuicidePopupExternal;
        }
        else
        {
            internalMsg = comp.ExecutionPopupInternal;
            externalMsg = comp.ExecutionPopupExternal;
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

    private bool CanExecuteWithAny(EntityUid weapon, EntityUid victim, EntityUid attacker)
    {
        // No point executing someone if they can't take damage
        if (!TryComp<DamageableComponent>(victim, out var damage))
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
        if (!CanExecuteWithAny(weapon, victim, user))
            return false;

        // We must be able to actually hurt people with the weapon
        if (!TryComp<MeleeWeaponComponent>(weapon, out var melee) && melee!.Damage.GetTotal() > 0.0f)
            return false;

        return true;
    }

    private bool CanExecuteWithGun(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!CanExecuteWithAny(weapon, victim, user))
            return false;

        // We must be able to actually fire the gun
        if (!TryComp<GunComponent>(weapon, out var gun) && _gunSystem.CanShoot(gun!))
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
            internalMsg = executionComp.SuicidePopupCompleteInternal;
            externalMsg = executionComp.SuicidePopupCompleteExternal;
        }
        else
        {
            internalMsg = executionComp.ExecutionPopupCompleteInternal;
            externalMsg = executionComp.ExecutionPopupCompleteExternal;
        }
        ShowExecutionInternalPopup(internalMsg, attacker, victim, weapon);
        ShowExecutionExternalPopup(externalMsg, attacker, victim, weapon);
    }

    private void OnDoafterGun(EntityUid uid, GunComponent comp, DoAfterEvent args)
    {
        if (!TryComp<ExecutionComponent>(uid, out var executionComp))
            return;

        if (!TryComp<TransformComponent>(args.Target, out var xform))
            return;

        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;

        var attacker = args.User;
        var weapon = args.Used.Value;
        var victim = args.Target.Value;

        if (!CanExecuteWithGun(weapon, victim, attacker))
            return;

        var active = EnsureComp<ActiveExecutionComponent>(uid);
        Dirty(uid, active);

        _gunSystem.AttemptShoot(args.User, uid, comp, xform.Coordinates);

        RemCompDeferred<ActiveExecutionComponent>(uid);
        Dirty(uid, active);

    }

    private void ShowExecutionInternalPopup(string locString,
        EntityUid attacker, EntityUid victim, EntityUid weapon)
    {
        _popupSystem.PopupClient(
            Loc.GetString(locString, ("attacker", attacker), ("victim", victim), ("weapon", weapon)),
            attacker,
            attacker,
            PopupType.Medium
            );

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

    private void OnAmmoShot(EntityUid uid, ActiveExecutionComponent comp, AmmoShotEvent args)
    {
        if (!TryComp<ExecutionComponent>(uid, out var executionComp))
            return;

        if (args.FiredProjectiles.Count < 1)
            return;

        // We only care about the first bullet
        var bullet = args.FiredProjectiles[0];

        if (!TryComp<ProjectileComponent>(bullet, out var projComponent))
            return;

        projComponent.Damage *= executionComp.DamageModifier;


        string internalMsg;
        string externalMsg;

        var attacker = comp.Attacker;
        var victim = comp.Victim;

        if (attacker == victim)
        {
            internalMsg = executionComp.SuicidePopupCompleteInternal;
            externalMsg = executionComp.SuicidePopupCompleteExternal;
        }
        else
        {
            internalMsg = executionComp.ExecutionPopupCompleteInternal;
            externalMsg = executionComp.ExecutionPopupCompleteExternal;
        }

        ShowExecutionInternalPopup(internalMsg, attacker, victim, uid);
        ShowExecutionExternalPopup(externalMsg, attacker, victim, uid);
    }
}

