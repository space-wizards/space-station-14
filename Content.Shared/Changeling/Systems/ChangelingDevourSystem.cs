using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Armor;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Changeling.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared.Changeling.Systems;

public sealed class ChangelingDevourSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedChangelingIdentitySystem _changelingIdentitySystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingDevourComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourActionEvent>(OnDevourAction);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourWindupDoAfterEvent>(OnDevourWindup);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourConsumeDoAfterEvent>(OnDevourConsume);
        SubscribeLocalEvent<ChangelingDevourComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<ChangelingDevourComponent> ent, ref MapInitEvent args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.ChangelingDevourActionEntity, ent.Comp.ChangelingDevourAction);
    }

    private void OnShutdown(Entity<ChangelingDevourComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ChangelingDevourActionEntity != null)
        {
            _actionsSystem.RemoveAction(ent.Owner, ent.Comp.ChangelingDevourActionEntity);
        }
    }

    // The action was used.
    // Start the first doafter for the windup.
    private void OnDevourAction(Entity<ChangelingDevourComponent> ent, ref ChangelingDevourActionEvent args)
    {
        if (args.Handled
            || _whitelistSystem.IsWhitelistFailOrNull(ent.Comp.Whitelist, args.Target)
            || !HasComp<ChangelingIdentityComponent>(ent))
            return;

        args.Handled = true;
        var target = args.Target;

        if (!CanDevour(ent.AsNullable(), target))
            return;

        if (_net.IsServer)
        {
            ent.Comp.CurrentDevourSound = _audio.Stop(ent.Comp.CurrentDevourSound);
            ent.Comp.CurrentDevourSound = _audio.PlayPvs(ent.Comp.DevourWindupNoise, ent)?.Entity;
        }

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ent:player} started changeling devour windup against {target:player}");

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, ent.Comp.DevourWindupTime, new ChangelingDevourWindupDoAfterEvent(), ent, target: target, used: ent)
        {
            BreakOnMove = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.None,
        });

        var selfMessage = Loc.GetString("changeling-devour-begin-windup-self", ("user", Identity.Entity(ent.Owner, EntityManager)));
        var othersMessage = Loc.GetString("changeling-devour-begin-windup-others", ("user", Identity.Entity(ent.Owner, EntityManager)));
        _popupSystem.PopupPredicted(
            selfMessage,
            othersMessage,
            args.Performer,
            args.Performer,
            PopupType.MediumCaution);
    }

    // First doafter finished.
    // Start the second doafter for the actual consumption and deal a small amount of damage.
    private void OnDevourWindup(Entity<ChangelingDevourComponent> ent, ref ChangelingDevourWindupDoAfterEvent args)
    {
        args.Handled = true;
        ent.Comp.CurrentDevourSound = _audio.Stop(ent.Comp.CurrentDevourSound);

        if (args.Cancelled)
            return;

        if (args.Target is not { } target)
            return;

        _damageable.ChangeDamage(target, ent.Comp.WindupDamage, true, true, ent.Owner);

        var selfMessage = Loc.GetString("changeling-devour-begin-consume-self", ("user", Identity.Entity(ent.Owner, EntityManager)));
        var othersMessage = Loc.GetString("changeling-devour-begin-consume-others", ("user", Identity.Entity(ent.Owner, EntityManager)));
        _popupSystem.PopupPredicted(
            selfMessage,
            othersMessage,
            ent.Owner,
            ent.Owner,
            PopupType.LargeCaution);

        if (_net.IsServer)
            ent.Comp.CurrentDevourSound = _audio.PlayPvs(ent.Comp.ConsumeNoise, ent)?.Entity;

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player} began to devour {ToPrettyString(target):player}'s identity");

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.DevourConsumeTime,
            new ChangelingDevourConsumeDoAfterEvent(),
            ent,
            target: target,
            used: ent)
        {
            BreakOnMove = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.None,
        });
    }

    // Second doafter finished.
    // Save the identity and deal more damage.
    private void OnDevourConsume(Entity<ChangelingDevourComponent> ent, ref ChangelingDevourConsumeDoAfterEvent args)
    {
        args.Handled = true;
        ent.Comp.CurrentDevourSound = _audio.Stop(ent.Comp.CurrentDevourSound);

        if (args.Cancelled)
            return;

        if (args.Target is not { } target)
            return;

        // Damage first before the CanDevour check to make sure they don't gib in-between and to kill them again in case they somehow revived.
        _damageable.ChangeDamage(target, ent.Comp.DevourDamage, true, true, ent.Owner);

        if (!CanDevour(ent.AsNullable(), target)) // Check again if the conditions are still met.
            return;

        var selfMessage = Loc.GetString("changeling-devour-consume-complete-self", ("user", Identity.Entity(ent.Owner, EntityManager)));
        var othersMessage = Loc.GetString("changeling-devour-consume-complete-others", ("user", Identity.Entity(ent.Owner, EntityManager)));
        _popupSystem.PopupPredicted(
            selfMessage,
            othersMessage,
            ent.Owner,
            ent.Owner,
            PopupType.LargeCaution);

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player} successfully devoured {ToPrettyString(target):player}'s identity");

        if (!TryComp<ChangelingIdentityComponent>(ent.Owner, out var identityStorage))
            return;

        _changelingIdentitySystem.CloneToPausedMap((ent, identityStorage), target);

        // We add a reference to ourselves to prevent repeated identity gain.
        var targetDevoured = EnsureComp<ChangelingDevouredComponent>(target);
        targetDevoured.DevouredBy.Add(ent.Owner);
        Dirty(target, targetDevoured);
        Dirty(ent);
    }

    /// <summary>
    /// Has the given victim been devoured by the given changeling before?
    /// </summary>
    public bool HasDevoured(Entity<ChangelingIdentityComponent?> changeling, EntityUid devoured)
    {
        if (!Resolve(changeling, ref changeling.Comp, false))
            return false;

        return changeling.Comp.ConsumedIdentities.ContainsValue(devoured);
    }

    /// <summary>
    /// Can the given changeling devour the given victim?
    /// </summary>
    public bool CanDevour(Entity<ChangelingDevourComponent?> changeling, EntityUid victim, bool showPopup = true)
    {
        if (!Resolve(changeling, ref changeling.Comp))
            return false;

        if (changeling.Owner == victim)
            return false; // Can't devour yourself.

        if (!HasComp<HumanoidProfileComponent>(victim))
        {
            if (showPopup)
                _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-cannot-devour"), changeling.Owner, changeling.Owner, PopupType.Medium);
            return false;
        }

        if (HasDevoured(changeling.Owner, victim))
        {
            if (showPopup)
                _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-already-devoured"), changeling.Owner, changeling.Owner, PopupType.Medium);
            return false;
        }

        if (!_mobState.IsDead(victim))
        {
            if (showPopup)
                _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-not-dead"), changeling.Owner, changeling.Owner, PopupType.Medium);
            return false;
        }

        if (HasComp<RottingComponent>(victim))
        {
            if (showPopup)
                _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-rotting"), changeling.Owner, changeling.Owner, PopupType.Medium);
            return false;
        }

        if (IsTargetProtected(victim, changeling!))
        {
            if (showPopup)
                _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-protected"), changeling.Owner, changeling.Owner, PopupType.Medium);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the target's outerclothing is beyond a DamageCoefficientThreshold to protect them from being devoured.
    /// </summary>
    /// <param name="target">The Targeted entity</param>
    /// <param name="ent">Changelings Devour Component</param>
    /// <returns>Is the target Protected from the attack</returns>
    private bool IsTargetProtected(EntityUid target, Entity<ChangelingDevourComponent> ent)
    {
        var ev = new CoefficientQueryEvent(SlotFlags.OUTERCLOTHING);

        RaiseLocalEvent(target, ev);

        foreach (var compProtectiveDamageType in ent.Comp.ProtectiveDamageTypes)
        {
            if (!ev.DamageModifiers.Coefficients.TryGetValue(compProtectiveDamageType, out var coefficient))
                continue;
            if (coefficient < 1f - ent.Comp.DevourPreventionPercentageThreshold)
                return true;
        }

        return false;
    }
}
