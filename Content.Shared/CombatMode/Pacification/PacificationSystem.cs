using System.Diagnostics.CodeAnalysis;
using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.CombatMode.Pacification;

public sealed class PacificationSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly INetManager _net = default!;

    //This data was previously stored in PacifiedComponent, determining when to show the popup to the player.
    //After refactoring to a status effect, it would be too difficult to store this data in all pacification effect components
    //(of which there may be many) and calculate the next popup time based on all of them.
    //And, in fact, there is no point in serializing and storing this data in the component,
    //since its only purpose is to prevent spam on the client. Therefore,
    //I decided that storing this data in the system is acceptable for this task.
    #region Data

    /// <summary>
    /// When attempting attack against the same entity multiple times,
    /// don't spam popups every frame and instead have a cooldown.
    /// </summary>
    public TimeSpan PopupCooldown = TimeSpan.FromSeconds(3.0);

    /// <summary>
    /// Time at which the next popup can be shown.
    /// </summary>
    public TimeSpan? NextPopupTime;

    /// <summary>
    /// The last entity attacked, used for popup purposes (avoid spam)
    /// </summary>
    public EntityUid? LastAttackedEntity;

    #endregion

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PacifiedStatusEffectComponent, StatusEffectAppliedEvent>(OnEffectApplied);
        SubscribeLocalEvent<PacifiedStatusEffectComponent, StatusEffectRemovedEvent>(OnEffectRemoved);

        SubscribeLocalEvent<PacifiedStatusEffectComponent, StatusEffectRelayedEvent<BeforeThrowEvent>>(OnBeforeThrow);
        SubscribeLocalEvent<PacifiedStatusEffectComponent, StatusEffectRelayedEvent<AttackAttemptEvent>>(OnAttackAttempt);
        SubscribeLocalEvent<PacifiedStatusEffectComponent, StatusEffectRelayedEvent<ShotAttemptedEvent>>(OnShootAttempt);

        SubscribeLocalEvent<PacifismDangerousAttackComponent, AttemptPacifiedAttackEvent>(OnPacifiedDangerousAttack);
    }

    private void OnEffectApplied(Entity<PacifiedStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        UpdatePacifiedSettings(args.Target);
    }

    private void OnEffectRemoved(Entity<PacifiedStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        UpdatePacifiedSettings(args.Target);
    }

    private void UpdatePacifiedSettings(EntityUid target)
    {
        if (!TryComp<CombatModeComponent>(target, out var combatMode))
            return;

        if (_statusEffect.TryEffectsWithComp<PacifiedStatusEffectComponent>(target, out var effects))
        {
            var disallowDisarm = false;
            var disallowAllCombat = false;
            foreach (var effect in effects)
            {
                if (effect.Comp1.DisallowDisarm)
                    disallowDisarm = true;

                if (effect.Comp1.DisallowAllCombat)
                    disallowAllCombat = true;
            }

            if (combatMode.CanDisarm != null)
                _combatSystem.SetCanDisarm(target, !disallowDisarm);

            _combatSystem.SetInCombatMode(target, !disallowAllCombat, combatMode);
            _actionsSystem.SetEnabled(combatMode.CombatToggleActionEntity, !disallowAllCombat);
        }
        else
        {
            if (combatMode.CanDisarm != null)
                _combatSystem.SetCanDisarm(target, true, combatMode);

            _actionsSystem.SetEnabled(combatMode.CombatToggleActionEntity, true);
        }
    }

    private void OnBeforeThrow(Entity<PacifiedStatusEffectComponent> ent, ref StatusEffectRelayedEvent<BeforeThrowEvent> args)
    {
        var thrownItem = args.Args.ItemUid;
        var itemName = Identity.Entity(thrownItem, EntityManager);

        // Raise an AttemptPacifiedThrow event and rely on other systems to check
        // whether the candidate item is OK to throw:
        var ev = new AttemptPacifiedThrowEvent(thrownItem, args.Args.PlayerUid);
        RaiseLocalEvent(thrownItem, ref ev);
        if (!ev.Cancelled)
            return;

        //C# disallows editing this after it has been passed by reference, so 3 line instead 1 line
        var throwArgs = args.Args;
        throwArgs.Cancelled = true;
        args.Args = throwArgs;

        // Tell the player why they can’t throw stuff:
        var cannotThrowMessage = ev.CancelReasonMessageId ?? "pacified-cannot-throw";
        _popup.PopupEntity(Loc.GetString(cannotThrowMessage, ("projectile", itemName)), args.Args.PlayerUid, args.Args.PlayerUid);
    }

    private void OnAttackAttempt(Entity<PacifiedStatusEffectComponent> ent, ref StatusEffectRelayedEvent<AttackAttemptEvent> args)
    {
        if (ent.Comp.DisallowAllCombat || args.Args.Disarm && ent.Comp.DisallowDisarm)
        {
            args.Args.Cancel();
            return;
        }

        // If it's a disarm, let it go through (unless we disallow them, which is handled earlier)
        if (args.Args.Disarm)
            return;

        // Allow attacking with no target. This should be fine.
        // If it's a wide swing, that will be handled with a later AttackAttemptEvent raise.
        if (args.Args.Target == null)
            return;

        // If we would do zero damage, it should be fine.
        if (args.Args.Weapon != null && args.Args.Weapon.Value.Comp.Damage.GetTotal() == FixedPoint2.Zero)
            return;

        if (PacifiedCanAttack(args.Args.Uid, args.Args.Target.Value, out var reason))
            return;

        ShowPopup(args.Args.Target.Value, reason);
        args.Args.Cancel();
    }

    private void OnShootAttempt(Entity<PacifiedStatusEffectComponent> ent, ref StatusEffectRelayedEvent<ShotAttemptedEvent> args)
    {
        if (HasComp<PacifismAllowedGunComponent>(args.Used))
            return;

        if (TryComp<BatteryWeaponFireModesComponent>(args.Used, out var component))
            if (component.FireModes[component.CurrentFireMode].PacifismAllowedMode)
                return;

        // Disallow firing guns in all cases.
        ShowPopup(ent, args.Used, "pacified-cannot-fire-gun");
        args.Cancel();
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

    private void ShowPopup(EntityUid popupTarget, string reason)
    {
        if (_net.IsServer)
            return;

        var player = _player.LocalEntity;

        // Popup logic.
        // Cooldown is needed because the input events for melee/shooting etc. will fire continuously
        if (popupTarget == LastAttackedEntity && !(_timing.CurTime > NextPopupTime))
            return;

        var targetName = Identity.Entity(popupTarget, EntityManager);
        _popup.PopupClient(Loc.GetString(reason, ("entity", targetName)), player);
        NextPopupTime = _timing.CurTime + PopupCooldown;
        LastAttackedEntity = popupTarget;
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
