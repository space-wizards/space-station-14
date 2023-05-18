using Content.Shared.ActionBlocker;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared.Actions;

public abstract class SharedActionsSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly RotateToFaceSystem _rotateToFaceSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionsComponent, DidEquipEvent>(OnDidEquip);
        SubscribeLocalEvent<ActionsComponent, DidEquipHandEvent>(OnHandEquipped);
        SubscribeLocalEvent<ActionsComponent, DidUnequipEvent>(OnDidUnequip);
        SubscribeLocalEvent<ActionsComponent, DidUnequipHandEvent>(OnHandUnequipped);

        SubscribeLocalEvent<ActionsComponent, ComponentGetState>(GetState);

        SubscribeAllEvent<RequestPerformActionEvent>(OnActionRequest);
    }

    #region ComponentStateManagement
    public virtual void Dirty(ActionType action)
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
        if (action.Toggled == toggled)
            return;

        action.Toggled = toggled;
        Dirty(action);
    }

    public void SetEnabled(ActionType action, bool enabled)
    {
        if (action.Enabled == enabled)
            return;

        action.Enabled = enabled;
        Dirty(action);
    }

    public void SetCharges(ActionType action, int? charges)
    {
        if (action.Charges == charges)
            return;

        action.Charges = charges;
        Dirty(action);
    }

    private void GetState(EntityUid uid, ActionsComponent component, ref ComponentGetState args)
    {
        args.State = new ActionsComponentState(component.Actions.ToList());
    }

    #endregion

    #region Execution
    /// <summary>
    ///     When receiving a request to perform an action, this validates whether the action is allowed. If it is, it
    ///     will raise the relevant <see cref="InstantActionEvent"/>
    /// </summary>
    private void OnActionRequest(RequestPerformActionEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        if (!TryComp(user, out ActionsComponent? component))
            return;

        // Does the user actually have the requested action?
        if (!component.Actions.TryGetValue(ev.Action, out var act))
        {
            _adminLogger.Add(LogType.Action,
                $"{ToPrettyString(user):user} attempted to perform an action that they do not have: {ev.Action.DisplayName}.");
            return;
        }

        if (!act.Enabled)
            return;

        var curTime = GameTiming.CurTime;
        if (act.Cooldown.HasValue && act.Cooldown.Value.End > curTime)
            return;

        BaseActionEvent? performEvent = null;

        // Validate request by checking action blockers and the like:
        var name = Loc.GetString(act.DisplayName);

        switch (act)
        {
            case EntityTargetAction entityAction:

                if (ev.EntityTarget is not { Valid: true } entityTarget)
                {
                    Logger.Error($"Attempted to perform an entity-targeted action without a target! Action: {entityAction.DisplayName}");
                    return;
                }

                var targetWorldPos = _transformSystem.GetWorldPosition(entityTarget);
                _rotateToFaceSystem.TryFaceCoordinates(user, targetWorldPos);

                if (!ValidateEntityTarget(user, entityTarget, entityAction))
                    return;

                if (act.Provider == null)
                    _adminLogger.Add(LogType.Action,
                    $"{ToPrettyString(user):user} is performing the {name:action} action targeted at {ToPrettyString(entityTarget):target}.");
                else
                    _adminLogger.Add(LogType.Action,
                    $"{ToPrettyString(user):user} is performing the {name:action} action (provided by {ToPrettyString(act.Provider.Value):provider}) targeted at {ToPrettyString(entityTarget):target}.");

                if (entityAction.Event != null)
                {
                    entityAction.Event.Target = entityTarget;
                    performEvent = entityAction.Event;
                }

                break;

            case WorldTargetAction worldAction:

                if (ev.EntityCoordinatesTarget is not { } entityCoordinatesTarget)
                {
                    Logger.Error($"Attempted to perform a world-targeted action without a target! Action: {worldAction.DisplayName}");
                    return;
                }

                _rotateToFaceSystem.TryFaceCoordinates(user, entityCoordinatesTarget.Position);

                if (!ValidateWorldTarget(user, entityCoordinatesTarget, worldAction))
                    return;

                if (act.Provider == null)
                    _adminLogger.Add(LogType.Action,
                        $"{ToPrettyString(user):user} is performing the {name:action} action targeted at {entityCoordinatesTarget:target}.");
                else
                    _adminLogger.Add(LogType.Action,
                        $"{ToPrettyString(user):user} is performing the {name:action} action (provided by {ToPrettyString(act.Provider.Value):provider}) targeted at {entityCoordinatesTarget:target}.");

                if (worldAction.Event != null)
                {
                    worldAction.Event.Target = entityCoordinatesTarget;
                    performEvent = worldAction.Event;
                }

                break;

            case InstantAction instantAction:

                if (act.CheckCanInteract && !_actionBlockerSystem.CanInteract(user, null))
                    return;

                if (act.Provider == null)
                    _adminLogger.Add(LogType.Action,
                        $"{ToPrettyString(user):user} is performing the {name:action} action.");
                else
                    _adminLogger.Add(LogType.Action,
                        $"{ToPrettyString(user):user} is performing the {name:action} action provided by {ToPrettyString(act.Provider.Value):provider}.");

                performEvent = instantAction.Event;
                break;
        }

        if (performEvent != null)
            performEvent.Performer = user;

        // All checks passed. Perform the action!
        PerformAction(user, component, act, performEvent, curTime);
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
        {
            // even if we don't check for obstructions, we may still need to check the range.
            var xform = Transform(user);
            var targetXform = Transform(target);

            if (xform.MapID != targetXform.MapID)
                return false;

            if (action.Range <= 0)
                return true;

            var distance = (_transformSystem.GetWorldPosition(xform) - _transformSystem.GetWorldPosition(targetXform)).Length;
            return distance <= action.Range;
        }

        if (_interactionSystem.InRangeUnobstructed(user, target, range: action.Range)
            && _containerSystem.IsInSameOrParentContainer(user, target))
        {
            return true;
        }

        return _interactionSystem.CanAccessViaStorage(user, target);
    }

    public bool ValidateWorldTarget(EntityUid user, EntityCoordinates coords, WorldTargetAction action)
    {
        if (action.CheckCanInteract && !_actionBlockerSystem.CanInteract(user, null))
            return false;

        if (!action.CheckCanAccess)
        {
            // even if we don't check for obstructions, we may still need to check the range.
            var xform = Transform(user);

            if (xform.MapID != coords.GetMapId(EntityManager))
                return false;

            if (action.Range <= 0)
                return true;

            return coords.InRange(EntityManager, _transformSystem, Transform(user).Coordinates, action.Range);
        }

        return _interactionSystem.InRangeUnobstructed(user, coords, range: action.Range);
    }

    public void PerformAction(EntityUid performer, ActionsComponent? component, ActionType action, BaseActionEvent? actionEvent, TimeSpan curTime, bool predicted = true)
    {
        var handled = false;

        var toggledBefore = action.Toggled;

        if (actionEvent != null)
        {
            // This here is required because of client-side prediction (RaisePredictiveEvent results in event re-use).
            actionEvent.Handled = false;

            if (action.Provider == null)
                RaiseLocalEvent(performer, (object) actionEvent, broadcast: true);
            else
                RaiseLocalEvent(action.Provider.Value, (object) actionEvent, broadcast: true);

            handled = actionEvent.Handled;
        }

        _audio.PlayPredicted(action.Sound, performer,predicted ? performer : null);
        handled |= action.Sound != null;

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

        if (dirty && component != null)
            Dirty(component);
    }
    #endregion

    #region AddRemoveActions
    /// <summary>
    ///     Add an action to an action component. If the entity has no action component, this will give them one.
    /// </summary>
    /// <param name="uid">Entity to receive the actions</param>
    /// <param name="action">The action to add</param>
    /// <param name="provider">The entity that enables these actions (e.g., flashlight). May be null (innate actions).</param>
    public virtual void AddAction(EntityUid uid, ActionType action, EntityUid? provider, ActionsComponent? comp = null, bool dirty = true)
    {
        // Because action classes have state data, e.g. cooldowns and uses-remaining, people should not be adding prototypes directly
        if (action is IPrototype)
        {
            Logger.Error("Attempted to directly add a prototype action. You need to clone a prototype in order to use it.");
            return;
        }

        comp ??= EnsureComp<ActionsComponent>(uid);
        action.Provider = provider;
        action.AttachedEntity = uid;
        AddActionInternal(comp, action);

        if (dirty)
            Dirty(comp);
    }

    protected virtual void AddActionInternal(ActionsComponent comp, ActionType action)
    {
        comp.Actions.Add(action);
    }

    /// <summary>
    ///     Add actions to an action component. If the entity has no action component, this will give them one.
    /// </summary>
    /// <param name="uid">Entity to receive the actions</param>
    /// <param name="actions">The actions to add</param>
    /// <param name="provider">The entity that enables these actions (e.g., flashlight). May be null (innate actions).</param>
    public void AddActions(EntityUid uid, IEnumerable<ActionType> actions, EntityUid? provider, ActionsComponent? comp = null, bool dirty = true)
    {
        comp ??= EnsureComp<ActionsComponent>(uid);

        var allClientExclusive = true;

        foreach (var action in actions)
        {
            AddAction(uid, action, provider, comp, false);
            allClientExclusive = allClientExclusive && action.ClientExclusive;
        }

        if (dirty && !allClientExclusive)
            Dirty(comp);
    }

    /// <summary>
    ///     Remove any actions that were enabled by some other entity. Useful when unequiping items that grant actions.
    /// </summary>
    public void RemoveProvidedActions(EntityUid uid, EntityUid provider, ActionsComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        foreach (var act in comp.Actions.ToArray())
        {
            if (act.Provider == provider)
                RemoveAction(uid, act, comp, dirty: false);
        }
        Dirty(comp);
    }

    public virtual void RemoveAction(EntityUid uid, ActionType action, ActionsComponent? comp = null, bool dirty = true)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        comp.Actions.Remove(action);
        action.AttachedEntity = null;

        if (dirty)
            Dirty(comp);
    }

    #endregion

    #region EquipHandlers
    private void OnDidEquip(EntityUid uid, ActionsComponent component, DidEquipEvent args)
    {
        var ev = new GetItemActionsEvent(args.SlotFlags);
        RaiseLocalEvent(args.Equipment, ev);

        if (ev.Actions.Count == 0)
            return;

        AddActions(args.Equipee, ev.Actions, args.Equipment, component);
    }

    private void OnHandEquipped(EntityUid uid, ActionsComponent component, DidEquipHandEvent args)
    {
        var ev = new GetItemActionsEvent();
        RaiseLocalEvent(args.Equipped, ev);

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
