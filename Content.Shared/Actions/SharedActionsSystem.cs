using Content.Shared.ActionBlocker;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared.Actions;

public abstract class SharedActionsSystem : EntitySystem
{
    [Dependency] private readonly SharedAdminLogSystem _logSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly RotateToFaceSystem _rotateToFaceSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionsComponent, DidEquipEvent>(OnDidEquip);
        SubscribeLocalEvent<ActionsComponent, DidEquipHandEvent>(OnHandEquipped);
        SubscribeLocalEvent<ActionsComponent, DidUnequipEvent>(OnDidUnequip);
        SubscribeLocalEvent<ActionsComponent, DidUnequipHandEvent>(OnHandUnequipped);

        SubscribeLocalEvent<ActionsComponent, ComponentGetState>(GetState);
        SubscribeLocalEvent<ActionsComponent, ComponentGetStateAttemptEvent>(OnCanGetState);

        SubscribeAllEvent<RequestPerformActionEvent>(OnActionRequest);
    }

    #region ComponentStateManagement
    protected virtual void Dirty(ActionType action)
    {
        if (action.AttachedEntity == null)
            return;

        if (!TryComp(action.AttachedEntity, out ActionsComponent? comp))
        {
            action.AttachedEntity = null;
            return;
        }

        Dirty(comp);
    }

    public void SetToggled(ActionType action, bool toggled)
    {
        action.Toggled = toggled;
        Dirty(action);
    }

    public void SetEnabled(ActionType action, bool enabled)
    {
        action.Enabled = enabled;
        Dirty(action);
    }

    public void SetCharges(ActionType action, int? charges)
    {
        action.Charges = charges;
        Dirty(action);
    }

    private void GetState(EntityUid uid, ActionsComponent component, ref ComponentGetState args)
    {
        args.State = new ActionsComponentState(component.Actions.ToList());
    }

    private void OnCanGetState(EntityUid uid, ActionsComponent component, ref ComponentGetStateAttemptEvent args)
    {
        // Only send action state data to the relevant player.
        if (args.Player.AttachedEntity != uid)
            args.Cancelled = true;
    }
    #endregion

    #region Execution
    /// <summary>
    ///     When receiving a request to perform an action, this validates whether the action is allowed. If it is, it
    ///     will raise the relevant <see cref="PerformActionEvent"/>
    /// </summary>
    private void OnActionRequest(RequestPerformActionEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not EntityUid user)
            return;

        if (!TryComp(user, out ActionsComponent? component))
            return;

        // Does the user actually have the requested action?
        if (!component.Actions.TryGetValue(ev.Action, out var act))
        {
            _logSystem.Add(LogType.Action,
                $"{ToPrettyString(user):user} attempted to perform an action that they do not have: {ev.Action.Name}.");
            return;
        }

        if (!act.Enabled)
            return;

        var curTime = GameTiming.CurTime;
        if (act.Cooldown.HasValue && act.Cooldown.Value.End > curTime)
            return;

        PerformActionEvent? performEvent = null;

        // Validate request by checking action blockers and the like:
        var name = Loc.GetString(act.Name);

        switch (act)
        {
            case EntityTargetAction entityAction:

                if (ev.EntityTarget is not EntityUid { Valid: true } entityTarget)
                {
                    Logger.Error($"Attempted to perform an entity-targeted action without a target! Action: {entityAction.Name}");
                    return;
                }

                _rotateToFaceSystem.TryFaceCoordinates(user, Transform(entityTarget).WorldPosition);

                if (!ValidateEntityTarget(user, entityTarget, entityAction))
                    return;

                if (act.Provider == null)
                    _logSystem.Add(LogType.Action,
                    $"{ToPrettyString(user):user} is performing the {name:action} action targeted at {ToPrettyString(entityTarget):target}.");
                else
                    _logSystem.Add(LogType.Action,
                    $"{ToPrettyString(user):user} is performing the {name:action} action (provided by {ToPrettyString(act.Provider.Value):provider}) targeted at {ToPrettyString(entityTarget):target}.");

                if (entityAction.Event != null)
                {
                    entityAction.Event.Target = entityTarget;
                    performEvent = entityAction.Event;
                }

                break;

            case WorldTargetAction worldAction:

                if (ev.MapTarget is not MapCoordinates mapTarget)
                {
                    Logger.Error($"Attempted to perform a map-targeted action without a target! Action: {worldAction.Name}");
                    return;
                }

                _rotateToFaceSystem.TryFaceCoordinates(user, mapTarget.Position);

                if (!ValidateWorldTarget(user, mapTarget, worldAction))
                    return;

                if (act.Provider == null)
                    _logSystem.Add(LogType.Action,
                        $"{ToPrettyString(user):user} is performing the {name:action} action targeted at {mapTarget:target}.");
                else
                    _logSystem.Add(LogType.Action,
                        $"{ToPrettyString(user):user} is performing the {name:action} action (provided by {ToPrettyString(act.Provider.Value):provider}) targeted at {mapTarget:target}.");

                if (worldAction.Event != null)
                {
                    worldAction.Event.Target = mapTarget;
                    performEvent = worldAction.Event;
                }

                break;

            case InstantAction instantAction:

                if (act.CheckCanInteract && !_actionBlockerSystem.CanInteract(user, null))
                    return;

                if (act.Provider == null)
                    _logSystem.Add(LogType.Action,
                        $"{ToPrettyString(user):user} is performing the {name:action} action.");
                else
                    _logSystem.Add(LogType.Action,
                        $"{ToPrettyString(user):user} is performing the {name:action} action provided by {ToPrettyString(act.Provider.Value):provider}.");

                performEvent = instantAction.Event;
                break;
        }

        if (performEvent != null)
            performEvent.Performer = user;

        // All checks passed. Perform the action!
        PerformAction(component, act, performEvent, curTime);
    }

    public bool ValidateEntityTarget(EntityUid user, EntityUid target, EntityTargetAction action)
    {
        if (!target.IsValid() || Deleted(target))
            return false;

        if (action.Whitelist != null && !action.Whitelist.IsValid(target, EntityManager))
            return false;

        if (action.CheckCanInteract && !_actionBlockerSystem.CanInteract(user, target))
            return false;

        if (user == target)
            return action.CanTargetSelf;

        if (!action.CheckCanAccess)
            return true;

        if (_interactionSystem.InRangeUnobstructed(user, target, range: action.Range)
            && _containerSystem.IsInSameOrParentContainer(user, target))
        {
            return true;
        }

        return _interactionSystem.CanAccessViaStorage(user, target);
    }

    public bool ValidateWorldTarget(EntityUid user, MapCoordinates coords, WorldTargetAction action)
    {
        if (coords == MapCoordinates.Nullspace)
            return false;

        if (action.CheckCanInteract && !_actionBlockerSystem.CanInteract(user, null))
            return false;

        return !action.CheckCanAccess || _interactionSystem.InRangeUnobstructed(user, coords, range: action.Range);
    }

    protected void PerformAction(ActionsComponent component, ActionType action, PerformActionEvent? actionEvent, TimeSpan curTime)
    {
        var handled = false;

        var toggledBefore = action.Toggled;

        if (actionEvent != null)
        {
            // This here is required because of client-side prediction (RaisePredictiveEvent results in event re-use).
            actionEvent.Handled = false;

            if (action.Provider == null)
                RaiseLocalEvent(component.Owner, (object) actionEvent, broadcast: true);
            else
                RaiseLocalEvent(action.Provider.Value, (object) actionEvent, broadcast: true);

            handled = actionEvent.Handled;
        }

        // Execute convenience functionality (pop-ups, sound, speech)
        handled |= PerformBasicActions(component.Owner, action);

        if (!handled)
            return; // no interaction occurred.

        // reduce charges, start cooldown, and mark as dirty (if required).

        var dirty = toggledBefore == action.Toggled;

        if (action.Charges != null)
        {
            dirty = true;
            action.Charges--;
            if (action.Charges == 0)
                action.Enabled = false;
        }

        action.Cooldown = null;
        if (action.UseDelay != null)
        {
            dirty = true;
            action.Cooldown = (curTime, curTime + action.UseDelay.Value);
        }

        if (dirty)
            Dirty(component);
    }

    /// <summary>
    ///     Execute convenience functionality for actions (pop-ups, sound, speech)
    /// </summary>
    protected virtual bool PerformBasicActions(EntityUid performer, ActionType action)
    {
        if (action.Sound == null && string.IsNullOrWhiteSpace(action.Popup))
            return false;

        var filter = Filter.Pvs(performer).RemoveWhereAttachedEntity(e => e == performer);

        if (action.Sound != null)
            SoundSystem.Play(filter, action.Sound.GetSound(), performer, action.AudioParams);

        if (string.IsNullOrWhiteSpace(action.Popup))
            return true;

        var msg = (!action.Toggled || string.IsNullOrWhiteSpace(action.PopupToggleSuffix))
            ? Loc.GetString(action.Popup)
            : Loc.GetString(action.Popup + action.PopupToggleSuffix);

        _popupSystem.PopupEntity(msg, performer, filter);

        return true;
    }
    #endregion

    #region AddRemoveActions
    /// <summary>
    ///     Add actions to an action component. If the entity has no action component, this will give them one.
    /// </summary>
    /// <param name="uid">Entity to receive the actions</param>
    /// <param name="actions">The actions</param>
    /// <param name="provider">The entity that enables these actions (e.g., flashlight). May be null (innate actions).</param>
    public virtual void AddActions(EntityUid uid, IEnumerable<ActionType> actions, EntityUid? provider, ActionsComponent? comp = null, bool dirty = true)
    {
        comp ??= EnsureComp<ActionsComponent>(uid);

        foreach (var action in actions)
        {
            action.Provider = provider;
            action.AttachedEntity = comp.Owner;
        }

        // Sometimes the client receives actions from the server, before predicting that newly added components will add
        // their own shared actions. Just in case those systems ever decided to directly access action properties (e.g.,
        // action.Toggled), we will remove duplicates:
        comp.Actions.ExceptWith(actions);

        comp.Actions.UnionWith(actions);

        // for client-exclusive actions, the client shouldn't mark the comp as dirty. Otherwise that just leads to
        // unnecessary prediction resetting and state handling.
        if (dirty)
            Dirty(comp);
    }

    /// <summary>
    ///     Remove any actions that were enabled by some other entity. Useful when unequiping items that grant actions.
    /// </summary>
    public void RemoveProvidedActions(EntityUid uid, EntityUid provider, ActionsComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        var provided = comp.Actions.Where(act => act.Provider == provider).ToList();

        RemoveActions(uid, provided, comp);
    }

    public virtual void RemoveActions(EntityUid uid, IEnumerable<ActionType> actions, ActionsComponent? comp = null, bool dirty = true)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        foreach (var action in actions)
        {
            comp.Actions.Remove(action);
            action.AttachedEntity = null;
        }

        if (dirty)
            Dirty(comp);
    }

    public void RemoveAction(EntityUid uid, ActionType action, ActionsComponent? comp = null)
        => RemoveActions(uid, new[] { action }, comp);

    public void AddAction(EntityUid uid, ActionType action, EntityUid? provider, ActionsComponent? comp = null)
        => AddActions(uid, new[] { action }, provider, comp);
    #endregion

    #region EquipHandlers
    private void OnDidEquip(EntityUid uid, ActionsComponent component, DidEquipEvent args)
    {
        var ev = new GetActionsEvent();
        RaiseLocalEvent(args.Equipment, ev, false);

        if (ev.Actions.Count == 0)
            return;

        AddActions(args.Equipee, ev.Actions, args.Equipment, component);
    }

    private void OnHandEquipped(EntityUid uid, ActionsComponent component, DidEquipHandEvent args)
    {
        var ev = new GetActionsEvent();
        RaiseLocalEvent(args.Equipped, ev, false);

        if (ev.Actions.Count == 0)
            return;

        AddActions(args.User, ev.Actions, args.Equipped, component);
    }

    private void OnDidUnequip(EntityUid uid, ActionsComponent component, DidUnequipEvent args)
    {
        RemoveProvidedActions(uid, args.Equipment, component);
    }

    private void OnHandUnequipped(EntityUid uid, ActionsComponent component, DidUnequipHandEvent args)
    {
        RemoveProvidedActions(uid, args.Unequipped, component);
    }
    #endregion
}
