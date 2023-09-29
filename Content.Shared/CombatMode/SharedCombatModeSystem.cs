using Content.Shared.Actions;
using Content.Shared.MouseRotator;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Targeting;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.CombatMode;

public abstract class SharedCombatModeSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private   readonly INetManager _netMan = default!;
    [Dependency] private   readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private   readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombatModeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CombatModeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CombatModeComponent, ToggleCombatActionEvent>(OnActionPerform);
    }

    private void OnMapInit(EntityUid uid, CombatModeComponent component, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref component.CombatToggleActionEntity, component.CombatToggleAction);
    }

    private void OnShutdown(EntityUid uid, CombatModeComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.CombatToggleActionEntity);

        SetMouseRotatorComponents(uid, false);
    }

    private void OnActionPerform(EntityUid uid, CombatModeComponent component, ToggleCombatActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        SetInCombatMode(uid, !component.IsInCombatMode, component);

        // TODO better handling of predicted pop-ups.
        // This probably breaks if the client has prediction disabled.

        if (!_netMan.IsClient || !Timing.IsFirstTimePredicted)
            return;

        var msg = component.IsInCombatMode ? "action-popup-combat-enabled" : "action-popup-combat-disabled";
        _popup.PopupEntity(Loc.GetString(msg), args.Performer, args.Performer);
    }

    public void SetCanDisarm(EntityUid entity, bool canDisarm, CombatModeComponent? component = null)
    {
        if (!Resolve(entity, ref component))
            return;

        component.CanDisarm = canDisarm;
    }

    public bool IsInCombatMode(EntityUid? entity, CombatModeComponent? component = null)
    {
        return entity != null && Resolve(entity.Value, ref component, false) && component.IsInCombatMode;
    }

    public virtual void SetInCombatMode(EntityUid entity, bool value, CombatModeComponent? component = null)
    {
        if (!Resolve(entity, ref component))
            return;

        if (component.IsInCombatMode == value)
            return;

        component.IsInCombatMode = value;
        Dirty(entity, component);

        if (component.CombatToggleActionEntity != null)
            _actionsSystem.SetToggled(component.CombatToggleActionEntity, component.IsInCombatMode);

        // Change mouse rotator comps if flag is set
        if (!component.ToggleMouseRotator)
            return;

        SetMouseRotatorComponents(entity, value);
    }

    public virtual void SetActiveZone(EntityUid entity, TargetingZone zone,
        CombatModeComponent? component = null)
    {
        if (!Resolve(entity, ref component))
            return;

        component.ActiveZone = zone;
    }

    private void SetMouseRotatorComponents(EntityUid uid, bool value)
    {
        if (value)
        {
            EnsureComp<MouseRotatorComponent>(uid);
            EnsureComp<NoRotateOnMoveComponent>(uid);
        }
        else
        {
            RemComp<MouseRotatorComponent>(uid);
            RemComp<NoRotateOnMoveComponent>(uid);
        }
    }
}

public sealed partial class ToggleCombatActionEvent : InstantActionEvent { }
