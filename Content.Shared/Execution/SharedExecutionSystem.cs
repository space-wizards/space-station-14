using Content.Shared.ActionBlocker;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Item.ItemToggle.Components;
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
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        InitialiseMelee();
        InitialiseGun();

        SubscribeLocalEvent<ItemToggleExecutionComponent, ItemToggledEvent>(OnItemToggleExecution);
        SubscribeLocalEvent<ExecutionComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionsVerbs);
        SubscribeLocalEvent<ExecutionComponent, ExecutionDoAfterEvent>(OnExecutionDoAfter);
    }

    /// <summary>
    /// Enable/Disable execution on a toggleable weapon (like an esword).
    /// </summary>
    private void OnItemToggleExecution(Entity<ItemToggleExecutionComponent> entityWithToggle, ref ItemToggledEvent args)
    {
        Entity<ExecutionComponent?> entityWithExecution = entityWithToggle.Owner;
        if (!Resolve(entityWithExecution.Owner, ref entityWithExecution.Comp))
            return;

        entityWithExecution.Comp.Enabled = args.Activated;
        DirtyField(entityWithExecution, nameof(ExecutionComponent.Enabled));
    }

    /// <summary>
    /// Answers the question of: can this attacker execute this victim with this weapon?
    /// This method stops you from executing already-dead people, executing unrestrained people, executing someone with a retracted esword, e.c.t.
    /// </summary>
    public bool CanBeExecuted(EntityUid victim, EntityUid attacker, Entity<ExecutionComponent> weapon)
    {
        // Can't execute something if the component says No!
        if (!weapon.Comp.Enabled)
            return false;

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

    /// <summary>
    /// Fetch the execute verb.
    /// <see cref="UtilityVerb"/> is a verb that is raised on an item held by an entity while examining another entity.
    /// So the verb is raised on the weapon you are going to use to execute someone.
    /// </summary>
    private void OnGetInteractionsVerbs(Entity<ExecutionComponent> entity, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return;

        var attacker = args.User;
        var victim = args.Target;

        if (!CanBeExecuted(victim, attacker, entity))
            return;

        UtilityVerb verb = new()
        {
            Act = () => TryStartExecutionDoAfter(victim, attacker, entity),
            Impact = LogImpact.High, // automatically makes an admin log
            Text = Loc.GetString("execution-verb-name"),
            Message = Loc.GetString("execution-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Start the Do-After to execute.
    /// </summary>
    private void TryStartExecutionDoAfter(EntityUid victim, EntityUid attacker, Entity<ExecutionComponent> weapon)
    {
        if (!CanBeExecuted(victim, attacker, weapon))
            return;

        var internalMessage = weapon.Comp.InternalMeleeExecutionMessage;
        var externalMessage = weapon.Comp.ExternalMeleeExecutionMessage;
        if (attacker == victim)
        {
            internalMessage = weapon.Comp.InternalSelfExecutionMessage;
            externalMessage = weapon.Comp.ExternalSelfExecutionMessage;
        }
        ShowPopups(internalMessage, externalMessage, attacker, victim, weapon);

        var doAfter =
            new DoAfterArgs(EntityManager, attacker, weapon.Comp.DoAfterDuration, new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        _doAfter.TryStartDoAfter(doAfter);
    }

    /// <summary>
    /// Simple helper method to create predicted popups.
    /// Why not use <see cref="SharedPopupSystem.PopupPredicted"/>? That shows the same message to everyone.
    /// We specifically need to show a different message to the person doing it and the people watching.
    /// </summary>
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

    /// <summary>
    /// Actually do the execution and kill the person.
    /// </summary>
    private void OnExecutionDoAfter(Entity<ExecutionComponent> entity, ref ExecutionDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;

        // can never be too careful
        if (!CanBeExecuted(args.Target.Value, args.User, entity))
            return;

        var ev = new BeforeExecutionEvent(args.User, args.Target.Value);
        RaiseLocalEvent(entity.Owner, ref ev);

        if (!ev.Handled)
            return;

        args.Handled = true;

        // if there is a harmless gun (like hos energy magnum set to disabler) it will play sound but do no damage
        if (ev.Sound is not null)
            _audio.PlayPredicted(ev.Sound, args.Target.Value, args.User);

        if (ev.Damage is null)
            return;

        // if it does stam damage then do a 100 stamina damage
        if (ev.Stamcrit)
            _stamina.TakeStaminaDamage(args.Target.Value, 100);

        if (ev.Damage.GetTotal() == 0)
            return;

        if (!TryComp<DamageableComponent>(args.Target.Value, out var damageable))
            return;

        // if we don't instakill (like practice round) just damage a bit and return early
        // no point showing the scary popups if they aren't going to die
        if (!ev.Instakill)
        {
            _damage.ChangeDamage(args.Target.Value, ev.Damage);
            return;
        }

        // this method does all the magic
        // it doesn't round-remove, it just multiplies the input damage specifier to be enough to kill the target
        _suicide.ApplyLethalDamage((args.Target.Value, damageable), ev.Damage);

        var internalMessage = entity.Comp.CompleteInternalMeleeExecutionMessage;
        var externalMessage = entity.Comp.CompleteExternalMeleeExecutionMessage;
        if (args.User == args.Target) // special messages for self-execution
        {
            internalMessage = entity.Comp.CompleteInternalSelfExecutionMessage;
            externalMessage = entity.Comp.CompleteExternalSelfExecutionMessage;
        }
        ShowPopups(internalMessage, externalMessage, args.User, args.Target.Value, entity.Owner);
    }
}

[Serializable, NetSerializable]
public sealed partial class ExecutionDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
/// Event called on the execution weapon before the execution occurs.
/// </summary>
/// <param name="Sound">Optional parameter, leave as null for the execution to be silent.</param>
/// The damage types the weapon deals. The actual numeric values don't matter, only the types; the victim will always die regardless.
/// If it is left null then the execution will fail and it will use the fail messages instead. If you include a sound that sound will still play but the victim won't be harmed.
/// Used for firing an empty revolver.
[ByRefEvent]
public record struct BeforeExecutionEvent(
    EntityUid Attacker,
    EntityUid Victim,
    bool Handled = false,
    SoundSpecifier? Sound = null,
    DamageSpecifier? Damage = null,
    bool Stamcrit = false,
    bool Instakill = true
);
