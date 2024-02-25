using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Network;
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
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatSystem = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _meleeSystem = default!;

    // TODO: Still needs more cleaning up.
    private const string DefaultInternalMeleeExecutionMessage = "execution-popup-melee-initial-internal";
    private const string DefaultExternalMeleeExecutionMessage = "execution-popup-melee-initial-external";
    private const string DefaultCompleteInternalMeleeExecutionMessage = "execution-popup-melee-complete-internal";
    private const string DefaultCompleteExternalMeleeExecutionMessage = "execution-popup-melee-complete-external";
    private const string DefaultInternalGunExecutionMessage = "execution-popup-gun-initial-internal";
    private const string DefaultExternalGunExecutionMessage = "execution-popup-gun-initial-external";
    private const string DefaultCompleteInternalGunExecutionMessage = "execution-popup-gun-complete-internal";
    private const string DefaultCompleteExternalGunExecutionMessage = "execution-popup-gun-complete-external";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExecutionComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionsVerbs);
        SubscribeLocalEvent<ExecutionComponent, ExecutionDoAfterEvent>(OnExecutionDoAfter);
        SubscribeLocalEvent<ExecutionComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
    }

    private void OnGetInteractionsVerbs(EntityUid uid, ExecutionComponent comp, GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return;

        var attacker = args.User;
        var weapon = args.Using.Value;
        var victim = args.Target;

        if (!CanExecuteWithAny(victim, attacker))
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
        if (!CanExecuteWithAny(victim, attacker))
            return;

        // TODO: This should just be on the weapons as a single execution message.
        var defaultExecutionInternal = DefaultInternalMeleeExecutionMessage;
        var defaultExecutionExternal = DefaultExternalMeleeExecutionMessage;

        if (HasComp<GunComponent>(weapon))
        {
            defaultExecutionExternal = DefaultInternalGunExecutionMessage;
            defaultExecutionInternal = DefaultExternalGunExecutionMessage;
        }

        var internalMsg = defaultExecutionInternal;
        var externalMsg = defaultExecutionExternal;
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
        // Use suicide.
        if (victim == attacker)
            return false;

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

    private void OnExecutionDoAfter(EntityUid uid, ExecutionComponent component, ExecutionDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;

        var attacker = args.User;
        var victim = args.Target.Value;
        var weapon = args.Used.Value;

        if (!CanExecuteWithAny(victim, attacker))
            return;

        // This is needed so the melee system does not stop it.
        var prev = _combatSystem.IsInCombatMode(attacker);
        _combatSystem.SetInCombatMode(attacker, true);
        component.Executing = true;
        string? internalMsg = null;
        string? externalMsg = null;

        if (TryComp(uid, out MeleeWeaponComponent? melee))
        {
            _meleeSystem.AttemptLightAttack(attacker, weapon, melee, victim);
            internalMsg = DefaultCompleteInternalMeleeExecutionMessage;
            externalMsg = DefaultCompleteExternalMeleeExecutionMessage;
        }
        else if (TryComp(uid, out GunComponent? gun))
        {
            var clumsyShot = false;

            // TODO: This should just be an event or something instead to get this.
            // TODO: Handle clumsy.
            if (!_gunSystem.AttemptDirectShoot(args.User, uid, args.Target.Value, gun))
            {
                internalMsg = null;
                externalMsg = null;
            }
            else
            {
                internalMsg = DefaultCompleteInternalGunExecutionMessage;
                externalMsg = DefaultCompleteExternalGunExecutionMessage;
            }
            args.Handled = true;
        }

        _combatSystem.SetInCombatMode(attacker, prev);
        component.Executing = false;
        args.Handled = true;

        if (internalMsg != null && externalMsg != null)
        {
            ShowExecutionInternalPopup(internalMsg, attacker, victim, uid);
            ShowExecutionExternalPopup(externalMsg, attacker, victim, uid);
        }
    }

    private void OnGetMeleeDamage(EntityUid uid, ExecutionComponent comp, ref GetMeleeDamageEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(uid, out var melee) ||
            !TryComp<ExecutionComponent>(uid, out var execComp) ||
            !execComp.Executing)
        {
            return;
        }

        var bonus = melee.Damage * execComp.DamageModifier - melee.Damage;
        args.Damage += bonus;
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
