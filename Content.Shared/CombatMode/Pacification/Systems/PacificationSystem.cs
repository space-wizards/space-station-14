using System.Diagnostics.CodeAnalysis;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Timing;

namespace Content.Shared.CombatMode.Pacification;

public sealed class PacificationSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PacifiedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PacifiedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PacifiedComponent, BeforeThrowEvent>(OnBeforeThrow);
        SubscribeLocalEvent<PacifiedComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<PacifiedComponent, ShotAttemptedEvent>(OnShootAttempt);
        SubscribeLocalEvent<PacifismDangerousAttackComponent, AttemptPacifiedAttackEvent>(OnPacifiedDangerousAttack);
    }

    private bool PacifiedCanAttack(EntityUid user, EntityUid target, [NotNullWhen(false)] out string? reason)
    {
        var ev = new AttemptPacifiedAttackEvent(user);

        RaiseLocalEvent(target, ref ev);

        if (ev.Cancelled)
        {
            reason = ev.Reason;
            return false;
        }

        reason = null;
        return true;
    }

    private void ShowPopup(Entity<PacifiedComponent> user, EntityUid target, string reason)
    {
        // Popup logic.
        // Cooldown is needed because the input events for melee/shooting etc. will fire continuously
        if (target == user.Comp.LastAttackedEntity
            && !(_timing.CurTime > user.Comp.NextPopupTime))
            return;

        var targetName = Identity.Entity(target, EntityManager);
        _popup.PopupClient(Loc.GetString(reason, ("entity", targetName)), user, user);
        user.Comp.NextPopupTime = _timing.CurTime + user.Comp.PopupCooldown;
        user.Comp.LastAttackedEntity = target;
    }

    private void OnShootAttempt(Entity<PacifiedComponent> ent, ref ShotAttemptedEvent args)
    {
        if (HasComp<PacifismAllowedGunComponent>(args.Used))
            return;

        // Disallow firing guns in all cases.
        ShowPopup(ent, args.Used, "pacified-cannot-fire-gun");
        args.Cancel();
    }

    private void OnAttackAttempt(EntityUid uid, PacifiedComponent component, AttackAttemptEvent args)
    {
        if (component.DisallowAllCombat || args.Disarm && component.DisallowDisarm)
        {
            args.Cancel();
            return;
        }

        // If it's a disarm, let it go through (unless we disallow them, which is handled earlier)
        if (args.Disarm)
            return;

        // Allow attacking with no target. This should be fine.
        // If it's a wide swing, that will be handled with a later AttackAttemptEvent raise.
        if (args.Target == null)
            return;

        // If we would do zero damage, it should be fine.
        if (args.Weapon != null && args.Weapon.Value.Comp.Damage.GetTotal() == FixedPoint2.Zero)
            return;

        if (PacifiedCanAttack(uid, args.Target.Value, out var reason))
            return;

        ShowPopup((uid, component), args.Target.Value, reason);
        args.Cancel();
    }

    private void OnStartup(EntityUid uid, PacifiedComponent component, ComponentStartup args)
    {
        if (!TryComp<CombatModeComponent>(uid, out var combatMode))
            return;

        if (component.DisallowDisarm && combatMode.CanDisarm != null)
            _combatSystem.SetCanDisarm(uid, false, combatMode);

        if (component.DisallowAllCombat)
        {
            _combatSystem.SetInCombatMode(uid, false, combatMode);
            _actionsSystem.SetEnabled(combatMode.CombatToggleActionEntity, false);
        }

        _alertsSystem.ShowAlert(uid, component.PacifiedAlert);
    }

    private void OnShutdown(EntityUid uid, PacifiedComponent component, ComponentShutdown args)
    {
        if (!TryComp<CombatModeComponent>(uid, out var combatMode))
            return;

        if (combatMode.CanDisarm != null)
            _combatSystem.SetCanDisarm(uid, true, combatMode);

        _actionsSystem.SetEnabled(combatMode.CombatToggleActionEntity, true);
        _alertsSystem.ClearAlert(uid, component.PacifiedAlert);
    }

    private void OnBeforeThrow(Entity<PacifiedComponent> ent, ref BeforeThrowEvent args)
    {
        var thrownItem = args.ItemUid;
        var itemName = Identity.Entity(thrownItem, EntityManager);

        // Raise an AttemptPacifiedThrow event and rely on other systems to check
        // whether the candidate item is OK to throw:
        var ev = new AttemptPacifiedThrowEvent(thrownItem, ent);
        RaiseLocalEvent(thrownItem, ref ev);
        if (!ev.Cancelled)
            return;

        args.Cancelled = true;

        // Tell the player why they can’t throw stuff:
        var cannotThrowMessage = ev.CancelReasonMessageId ?? "pacified-cannot-throw";
        _popup.PopupEntity(Loc.GetString(cannotThrowMessage, ("projectile", itemName)), ent, ent);
    }

    private void OnPacifiedDangerousAttack(Entity<PacifismDangerousAttackComponent> ent, ref AttemptPacifiedAttackEvent args)
    {
        args.Cancelled = true;
        args.Reason = "pacified-cannot-harm-indirect";
    }
}


/// <summary>
/// Raised when a Pacified entity attempts to throw something.
/// The throw is only permitted if this event is not cancelled.
/// </summary>
[ByRefEvent]
public struct AttemptPacifiedThrowEvent
{
    public EntityUid ItemUid;
    public EntityUid PlayerUid;

    public AttemptPacifiedThrowEvent(EntityUid itemUid,  EntityUid playerUid)
    {
        ItemUid = itemUid;
        PlayerUid = playerUid;
    }

    public bool Cancelled { get; private set; } = false;
    public string? CancelReasonMessageId { get; private set; }

    /// <param name="reasonMessageId">
    /// Localization string ID for the reason this event has been cancelled.
    /// If null, a generic message will be shown to the player.
    /// Note that any supplied localization string MUST accept a '$projectile'
    /// parameter specifying the name of the thrown entity.
    /// </param>
    public void Cancel(string? reasonMessageId = null)
    {
        Cancelled = true;
        CancelReasonMessageId = reasonMessageId;
    }
}

/// <summary>
///     Raised ref directed on an entity when a pacified user is attempting to attack it.
///     If <see cref="Cancelled"/> is true, don't allow attacking.
///     <see cref="Reason"/> should be a loc string, if there needs to be special text for why the user isn't able to attack this.
/// </summary>
[ByRefEvent]
public record struct AttemptPacifiedAttackEvent(EntityUid User, bool Cancelled = false, string Reason = "pacified-cannot-harm-directly");
