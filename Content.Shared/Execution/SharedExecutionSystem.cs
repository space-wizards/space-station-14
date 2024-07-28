using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Player;

namespace Content.Shared.Execution;

/// <summary>
///     Verb for violently murdering cuffed creatures.
/// </summary>
public sealed class SharedExecutionSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExecutionComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionsVerbs);
        SubscribeLocalEvent<ExecutionComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<ExecutionComponent, SuicideEvent>(OnSuicide);
    }

    private void OnGetInteractionsVerbs(EntityUid uid, ExecutionComponent comp, GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return;

        var attacker = args.User;
        var weapon = args.Using.Value;
        var victim = args.Target;

        if (!CanBeExecuted(victim, attacker))
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
        if (!CanBeExecuted(victim, attacker))
            return;

        // TODO: This should just be on the weapons as a single execution message.
        var defaultExecutionInternal = comp.DefaultInternalMeleeExecutionMessage;
        var defaultExecutionExternal = comp.DefaultExternalMeleeExecutionMessage;

        var internalMsg = defaultExecutionInternal;
        var externalMsg = defaultExecutionExternal;
        ShowExecutionInternalPopup(internalMsg, attacker, victim, weapon);
        ShowExecutionExternalPopup(externalMsg, attacker, victim, weapon);

        var doAfter =
            new DoAfterArgs(EntityManager, attacker, comp.DoAfterDuration, new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        _doAfter.TryStartDoAfter(doAfter);

    }

    public bool CanBeExecuted(EntityUid victim, EntityUid attacker)
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
        args.ResistanceBypass = true;
    }

    private void OnSuicide(EntityUid uid, ExecutionComponent comp, ref SuicideEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(uid, out var melee))
            return;

        args.Damage = melee.Damage;
        args.Handled = true;
    }

    public void ShowExecutionInternalPopup(string locString,
        EntityUid attacker, EntityUid victim, EntityUid weapon, bool predict = true)
    {
        if (predict)
        {
            _popup.PopupClient(
                Loc.GetString(locString, ("attacker", attacker), ("victim", victim), ("weapon", weapon)),
                attacker,
                attacker,
                PopupType.MediumCaution
                );
        }
        else
        {
            _popup.PopupEntity(
                Loc.GetString(locString, ("attacker", attacker), ("victim", victim), ("weapon", weapon)),
                attacker,
                Filter.Entities(attacker),
                true,
                PopupType.MediumCaution
            );
        }
    }

    public void ShowExecutionExternalPopup(string locString, EntityUid attacker, EntityUid victim, EntityUid weapon)
    {
        _popup.PopupEntity(
            Loc.GetString(locString, ("attacker", attacker), ("victim", victim), ("weapon", weapon)),
            attacker,
            Filter.PvsExcept(attacker),
            true,
            PopupType.MediumCaution
            );
    }
}
