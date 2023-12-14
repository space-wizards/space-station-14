using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Throwing;

namespace Content.Shared.CombatMode.Pacification;

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

public sealed class PacificationSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PacifiedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PacifiedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PacifiedComponent, BeforeThrowEvent>(OnBeforeThrow);
        SubscribeLocalEvent<PacifiedComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    private void OnAttackAttempt(EntityUid uid, PacifiedComponent component, AttackAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnStartup(EntityUid uid, PacifiedComponent component, ComponentStartup args)
    {
        if (!TryComp<CombatModeComponent>(uid, out var combatMode))
            return;

        if (combatMode.CanDisarm != null)
            _combatSystem.SetCanDisarm(uid, false, combatMode);

        _combatSystem.SetInCombatMode(uid, false, combatMode);
        _actionsSystem.SetEnabled(combatMode.CombatToggleActionEntity, false);
        _alertsSystem.ShowAlert(uid, AlertType.Pacified);
    }

    private void OnShutdown(EntityUid uid, PacifiedComponent component, ComponentShutdown args)
    {
        if (!TryComp<CombatModeComponent>(uid, out var combatMode))
            return;

        if (combatMode.CanDisarm != null)
            _combatSystem.SetCanDisarm(uid, true, combatMode);

        _actionsSystem.SetEnabled(combatMode.CombatToggleActionEntity, true);
        _alertsSystem.ClearAlert(uid, AlertType.Pacified);
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
}
