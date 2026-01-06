using Content.Shared.ActionBlocker;
using Content.Shared.Chat;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Execution;

/// <summary>
///     Verb for violently murdering cuffed creatures.
/// </summary>
public sealed partial class SharedExecutionSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSuicideSystem _suicide = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        InitialiseMelee();

        SubscribeLocalEvent<ExecutionComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionsVerbs);
        SubscribeLocalEvent<ExecutionComponent, ExecutionDoAfterEvent>(OnExecutionDoAfter);
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

        var internalMessage = comp.InternalMeleeExecutionMessage;
        var externalMessage = comp.ExternalMeleeExecutionMessage;
        if (attacker == victim)
        {
            internalMessage = comp.InternalSelfExecutionMessage;
            externalMessage = comp.ExternalSelfExecutionMessage;
        }
        ShowPopups(internalMessage, externalMessage, attacker, victim, weapon);

        var doAfter =
            new DoAfterArgs(EntityManager, attacker, comp.DoAfterDuration, new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void ShowPopups(string internalMessage, string externalMessage, EntityUid attacker, EntityUid victim, EntityUid weapon)
    {
        // popup for self
        _popup.PopupClient(
            Loc.GetString(internalMessage, ("attacker", Identity.Entity(attacker, EntityManager)), ("victim", Identity.Entity(victim, EntityManager)), ("weapon", weapon)),
            attacker,
            PopupType.MediumCaution
        );

        // popup for others
        _popup.PopupEntity(
            Loc.GetString(externalMessage, ("attacker", Identity.Entity(attacker, EntityManager)), ("victim", Identity.Entity(victim, EntityManager)), ("weapon", weapon)),
            attacker,
            Filter.PvsExcept(attacker),
            true,
            PopupType.MediumCaution
        );
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

    private void OnExecutionDoAfter(Entity<ExecutionComponent> entity, ref ExecutionDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;

        if (!CanBeExecuted(args.Target.Value, args.User))
            return;

        var ev = new BeforeExecutionEvent();
        RaiseLocalEvent(entity.Owner, ref ev);

        if (!ev.Handled)
            return;

        if (ev.Sound is not null)
            _audio.PlayPredicted(ev.Sound, args.Target.Value, args.User);

        if (ev.Damage is null)
            return;

        if (!TryComp<DamageableComponent>(args.Target.Value, out var damageable))
            return;

        _suicide.ApplyLethalDamage((args.Target.Value, damageable), ev.Damage);

        var internalMessage = entity.Comp.CompleteInternalMeleeExecutionMessage;
        var externalMessage = entity.Comp.CompleteExternalMeleeExecutionMessage;
        if (args.User == args.Target)
        {
            internalMessage = entity.Comp.CompleteInternalSelfExecutionMessage;
            externalMessage = entity.Comp.CompleteExternalSelfExecutionMessage;
        }
        ShowPopups(internalMessage, externalMessage, args.User, args.Target.Value, entity.Owner);

        args.Handled = true;
    }
}

[Serializable, NetSerializable]
public sealed partial class ExecutionDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
/// Event called on the execution weapon before the execution occurs.
/// </summary>
/// <param name="Handled"></param>
/// <param name="Sound">Optional parameter, leave as null for the execution to be silent.</param>
/// <param name="Damage">
/// The damage types the weapon deals. The actual numeric values don't matter, only the types; the victim will always die regardless.
/// If it is left null then the execution will fail and it will use the fail messages instead. If you include a sound that sound will still play but the victim won't be harmed.
/// Used for firing an empty revolver.
/// </param>
[ByRefEvent]
public record struct BeforeExecutionEvent(bool Handled = false, SoundSpecifier? Sound = null, DamageSpecifier? Damage = null);
