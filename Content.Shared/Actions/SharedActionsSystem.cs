using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

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
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionsComponent, DidEquipEvent>(OnDidEquip);
        SubscribeLocalEvent<ActionsComponent, DidEquipHandEvent>(OnHandEquipped);
        SubscribeLocalEvent<ActionsComponent, DidUnequipEvent>(OnDidUnequip);
        SubscribeLocalEvent<ActionsComponent, DidUnequipHandEvent>(OnHandUnequipped);

        SubscribeLocalEvent<ActionsComponent, ComponentGetState>(OnActionsGetState);

        SubscribeLocalEvent<InstantActionComponent, ComponentGetState>(OnInstantGetState);
        SubscribeLocalEvent<EntityTargetActionComponent, ComponentGetState>(OnEntityTargetGetState);
        SubscribeLocalEvent<WorldTargetActionComponent, ComponentGetState>(OnWorldTargetGetState);

        SubscribeLocalEvent<InstantActionComponent, ComponentHandleState>(OnInstantHandleState);
        SubscribeLocalEvent<EntityTargetActionComponent, ComponentHandleState>(OnEntityTargetHandleState);
        SubscribeLocalEvent<WorldTargetActionComponent, ComponentHandleState>(OnWorldTargetHandleState);

        SubscribeLocalEvent<InstantActionComponent, GetActionDataEvent>(OnGetActionData);
        SubscribeLocalEvent<EntityTargetActionComponent, GetActionDataEvent>(OnGetActionData);
        SubscribeLocalEvent<WorldTargetActionComponent, GetActionDataEvent>(OnGetActionData);

        SubscribeAllEvent<RequestPerformActionEvent>(OnActionRequest);
    }

    private void OnInstantGetState(EntityUid uid, InstantActionComponent component, ref ComponentGetState args)
    {
        args.State = new InstantActionComponentState(component, EntityManager);
    }

    private void OnEntityTargetGetState(EntityUid uid, EntityTargetActionComponent component, ref ComponentGetState args)
    {
        args.State = new EntityTargetActionComponentState(component, EntityManager);
    }

    private void OnWorldTargetGetState(EntityUid uid, WorldTargetActionComponent component, ref ComponentGetState args)
    {
        args.State = new WorldTargetActionComponentState(component, EntityManager);
    }

    private void BaseHandleState<T>(EntityUid uid, BaseActionComponent component, BaseActionComponentState state) where T : BaseActionComponent
    {
        component.Icon = state.Icon;
        component.IconOn = state.IconOn;
        component.IconColor = state.IconColor;
        component.Keywords = new HashSet<string>(state.Keywords);
        component.Enabled = state.Enabled;
        component.Toggled = state.Toggled;
        component.Cooldown = state.Cooldown;
        component.UseDelay = state.UseDelay;
        component.Charges = state.Charges;
        component.Container = EnsureEntity<T>(state.Container, uid);
        component.EntityIcon = EnsureEntity<T>(state.EntityIcon, uid);
        component.CheckCanInteract = state.CheckCanInteract;
        component.ClientExclusive = state.ClientExclusive;
        component.Priority = state.Priority;
        component.AttachedEntity = EnsureEntity<T>(state.AttachedEntity, uid);
        component.AutoPopulate = state.AutoPopulate;
        component.Temporary = state.Temporary;
        component.ItemIconStyle = state.ItemIconStyle;
        component.Sound = state.Sound;
    }

    private void OnInstantHandleState(EntityUid uid, InstantActionComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not InstantActionComponentState state)
            return;

        BaseHandleState<InstantActionComponent>(uid, component, state);
    }

    private void OnEntityTargetHandleState(EntityUid uid, EntityTargetActionComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not EntityTargetActionComponentState state)
            return;

        BaseHandleState<EntityTargetActionComponent>(uid, component, state);
        component.Whitelist = state.Whitelist;
        component.CanTargetSelf = state.CanTargetSelf;
    }

    private void OnWorldTargetHandleState(EntityUid uid, WorldTargetActionComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not WorldTargetActionComponentState state)
            return;

        BaseHandleState<WorldTargetActionComponent>(uid, component, state);
    }

    private void OnGetActionData<T>(EntityUid uid, T component, ref GetActionDataEvent args) where T : BaseActionComponent
    {
        args.Action = component;
    }

    public bool TryGetActionData(
        [NotNullWhen(true)] EntityUid? uid,
        [NotNullWhen(true)] out BaseActionComponent? result,
        bool logError = true)
    {
        result = null;
        if (!Exists(uid))
            return false;

        var ev = new GetActionDataEvent();
        RaiseLocalEvent(uid.Value, ref ev);
        result = ev.Action;

        if (result != null)
            return true;

        Log.Error($"Failed to get action from action entity: {ToPrettyString(uid.Value)}");
        return false;
    }

    public bool ResolveActionData(
        [NotNullWhen(true)] EntityUid? uid,
        [NotNullWhen(true)] ref BaseActionComponent? result,
        bool logError = true)
    {
        if (result != null)
        {
            DebugTools.Assert(result.Owner == uid);
            return true;
        }

        return TryGetActionData(uid, out result, logError);
    }

    public void SetCooldown(EntityUid? actionId, TimeSpan start, TimeSpan end)
    {
        if (actionId == null)
            return;

        if (!TryGetActionData(actionId, out var action))
            return;

        action.Cooldown = (start, end);
        Dirty(actionId.Value, action);
    }

    #region ComponentStateManagement
    public virtual void Dirty(EntityUid? actionId)
    {
        if (!TryGetActionData(actionId, out var action))
            return;

        Dirty(actionId.Value, action);

        if (action.AttachedEntity == null)
            return;

        var ent = action.AttachedEntity;

        if (!TryComp(ent, out ActionsComponent? comp))
        {
            action.AttachedEntity = null;
            return;
        }

        Dirty(action.AttachedEntity.Value, comp);
    }

    public void SetToggled(EntityUid? actionId, bool toggled)
    {
        if (!TryGetActionData(actionId, out var action) ||
            action.Toggled == toggled)
        {
            return;
        }

        action.Toggled = toggled;
        Dirty(actionId.Value, action);
    }

    public void SetEnabled(EntityUid? actionId, bool enabled)
    {
        if (!TryGetActionData(actionId, out var action) ||
            action.Enabled == enabled)
        {
            return;
        }

        action.Enabled = enabled;
        Dirty(actionId.Value, action);
    }

    public void SetCharges(EntityUid? actionId, int? charges)
    {
        if (!TryGetActionData(actionId, out var action) ||
            action.Charges == charges)
        {
            return;
        }

        action.Charges = charges;
        Dirty(actionId.Value, action);
    }

    private void OnActionsGetState(EntityUid uid, ActionsComponent component, ref ComponentGetState args)
    {
        args.State = new ActionsComponentState(GetNetEntitySet(component.Actions));
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

        var actionEnt = GetEntity(ev.Action);

        if (!TryComp(actionEnt, out MetaDataComponent? metaData))
            return;

        var name = Name(actionEnt, metaData);

        // Does the user actually have the requested action?
        if (!component.Actions.Contains(actionEnt))
        {
            _adminLogger.Add(LogType.Action,
                $"{ToPrettyString(user):user} attempted to perform an action that they do not have: {name}.");
            return;
        }

        if (!TryGetActionData(actionEnt, out var action))
            return;

        if (!action.Enabled)
            return;

        var curTime = GameTiming.CurTime;
        if (action.Cooldown.HasValue && action.Cooldown.Value.End > curTime)
            return;

        BaseActionEvent? performEvent = null;

        // Validate request by checking action blockers and the like:
        switch (action)
        {
            case EntityTargetActionComponent entityAction:
                if (ev.EntityTarget is not { Valid: true } netTarget)
                {
                    Log.Error($"Attempted to perform an entity-targeted action without a target! Action: {name}");
                    return;
                }

                var entityTarget = GetEntity(netTarget);

                var targetWorldPos = _transformSystem.GetWorldPosition(entityTarget);
                _rotateToFaceSystem.TryFaceCoordinates(user, targetWorldPos);

                if (!ValidateEntityTarget(user, entityTarget, entityAction))
                    return;

                _adminLogger.Add(LogType.Action,
                    $"{ToPrettyString(user):user} is performing the {name:action} action (provided by {ToPrettyString(action.Container):provider}) targeted at {ToPrettyString(entityTarget):target}.");

                if (entityAction.Event != null)
                {
                    entityAction.Event.Target = entityTarget;
                    Dirty(actionEnt, entityAction);
                    performEvent = entityAction.Event;
                }

                break;
            case WorldTargetActionComponent worldAction:
                if (ev.EntityCoordinatesTarget is not { } netCoordinatesTarget)
                {
                    Log.Error($"Attempted to perform a world-targeted action without a target! Action: {name}");
                    return;
                }

                var entityCoordinatesTarget = GetCoordinates(netCoordinatesTarget);
                _rotateToFaceSystem.TryFaceCoordinates(user, entityCoordinatesTarget.Position);

                if (!ValidateWorldTarget(user, entityCoordinatesTarget, worldAction))
                    return;

                _adminLogger.Add(LogType.Action,
                    $"{ToPrettyString(user):user} is performing the {name:action} action (provided by {ToPrettyString(action.Container):provider}) targeted at {entityCoordinatesTarget:target}.");

                if (worldAction.Event != null)
                {
                    worldAction.Event.Target = entityCoordinatesTarget;
                    Dirty(actionEnt, worldAction);
                    performEvent = worldAction.Event;
                }

                break;
            case InstantActionComponent instantAction:
                if (action.CheckCanInteract && !_actionBlockerSystem.CanInteract(user, null))
                    return;

                _adminLogger.Add(LogType.Action,
                    $"{ToPrettyString(user):user} is performing the {name:action} action provided by {ToPrettyString(action.Container):provider}.");

                performEvent = instantAction.Event;
                break;
        }

        if (performEvent != null)
            performEvent.Performer = user;

        // All checks passed. Perform the action!
        PerformAction(user, component, actionEnt, action, performEvent, curTime);
    }

    public bool ValidateEntityTarget(EntityUid user, EntityUid target, EntityTargetActionComponent action)
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

            var distance = (_transformSystem.GetWorldPosition(xform) - _transformSystem.GetWorldPosition(targetXform)).Length();
            return distance <= action.Range;
        }

        if (_interactionSystem.InRangeUnobstructed(user, target, range: action.Range)
            && _containerSystem.IsInSameOrParentContainer(user, target))
        {
            return true;
        }

        return _interactionSystem.CanAccessViaStorage(user, target);
    }

    public bool ValidateWorldTarget(EntityUid user, EntityCoordinates coords, WorldTargetActionComponent action)
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

    public void PerformAction(EntityUid performer, ActionsComponent? component, EntityUid actionId, BaseActionComponent action, BaseActionEvent? actionEvent, TimeSpan curTime, bool predicted = true)
    {
        var handled = false;

        var toggledBefore = action.Toggled;

        if (actionEvent != null)
        {
            // This here is required because of client-side prediction (RaisePredictiveEvent results in event re-use).
            actionEvent.Handled = false;
            RaiseLocalEvent(action.Container, (object) actionEvent, broadcast: true);
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

        Dirty(actionId, action);

        if (dirty && component != null)
            Dirty(performer, component);
    }
    #endregion

    #region AddRemoveActions

    public EntityUid? AddAction(EntityUid performer,
        string? actionPrototypeId,
        EntityUid container = default,
        ActionsComponent? component = null)
    {
        EntityUid? actionId = null;
        AddAction(performer, ref actionId, out _, actionPrototypeId, container, component);
        return actionId;
    }

    /// <summary>
    ///     Adds an action to an action holder. If the given entity does not exist, it will attempt to spawn one.
    ///     If the holder has no actions component, this will give them one.
    /// </summary>
    public bool AddAction(EntityUid performer,
        [NotNullWhen(true)] ref EntityUid? actionId,
        string? actionPrototypeId,
        EntityUid container = default,
        ActionsComponent? component = null)
    {
        return AddAction(performer, ref actionId, out _, actionPrototypeId, container, component);
    }

    /// <inheritdoc cref="AddAction(Robust.Shared.GameObjects.EntityUid,ref System.Nullable{Robust.Shared.GameObjects.EntityUid},string?,Robust.Shared.GameObjects.EntityUid,Content.Shared.Actions.ActionsComponent?)"/>
    public bool AddAction(EntityUid performer,
        [NotNullWhen(true)] ref EntityUid? actionId,
        [NotNullWhen(true)] out BaseActionComponent? action,
        string? actionPrototypeId,
        EntityUid container = default,
        ActionsComponent? component = null)
    {
        if (!container.IsValid())
            container = performer;

        if (!_actionContainer.EnsureAction(container, ref actionId, out action, actionPrototypeId))
            return false;

        return GrantAction(performer, actionId.Value, component, action);
    }

    /// <summary>
    ///     Grants a user a pre-existing action.
    ///     If the holder has no actions component, this will give them one.
    /// </summary>
    /// <param name="performer">Entity to receive the actions</param>
    /// <param name="actionId">Action entity to add</param>
    /// <param name="comp">Component of <see cref="holderId"/></param>
    /// <param name="action">Component of <see cref="actionId"/></param>
    /// <param name="provider">The entity that enables these actions (e.g., flashlight). May be null (innate actions).</param>
    /// <param name="actionContainer">Action container of <see cref="holderId"/></param>
    public bool GrantAction(EntityUid performer,
        EntityUid actionId,
        ActionsComponent? comp = null,
        BaseActionComponent? action = null)
    {
        if (!ResolveActionData(actionId, ref action))
            return false;

        if (!TryComp(action.Container, out ActionsContainerComponent? containerComp)
            || !containerComp.Container.Contains(actionId))
        {
            Log.Error($"Attempted to add an action with an invalid container: {ToPrettyString(actionId)}");
            return false;
        }

        DebugTools.Assert(comp == null || comp.Owner == performer);
        comp ??= EnsureComp<ActionsComponent>(performer);
        action.AttachedEntity = performer;
        comp.Actions.Add(actionId);
        Dirty(actionId, action);
        Dirty(performer, comp);
        ActionAdded(performer, actionId, comp, action);
        return true;
    }

    protected virtual void ActionAdded(EntityUid performer, EntityUid actionId, ActionsComponent comp, BaseActionComponent action)
    {
        // See client-side system for UI code.
    }

    /// <summary>
    ///     Grant pre-existing actions. If the entity has no action component, this will give them one.
    /// </summary>
    /// <param name="performer">Entity to receive the actions</param>
    /// <param name="actions">The actions to add</param>
    /// <param name="container">The entity that enables these actions (e.g., flashlight). May be null (innate actions).</param>
    public void GrantActions(EntityUid performer, IEnumerable<EntityUid> actions, EntityUid container, ActionsComponent? comp = null)
    {
        DebugTools.Assert(comp == null || comp.Owner == performer);
        comp ??= EnsureComp<ActionsComponent>(performer);

        foreach (var actionId in actions)
        {
            GrantAction(performer, actionId, comp);
        }
    }

    public IEnumerable<(EntityUid Id, BaseActionComponent Comp)> GetActions(EntityUid holderId, ActionsComponent? actions = null)
    {
        if (!Resolve(holderId, ref actions, false))
            yield break;

        foreach (var actionId in actions.Actions)
        {
            if (!TryGetActionData(actionId, out var action))
                continue;

            yield return (actionId, action);
        }
    }

    /// <summary>
    ///     Remove any actions that were enabled by some other entity. Useful when unequiping items that grant actions.
    /// </summary>
    public void RemoveProvidedActions(EntityUid performer, EntityUid provider, ActionsComponent? comp = null)
    {
        if (!Resolve(performer, ref comp, false))
            return;

        foreach (var actionId in comp.Actions.ToArray())
        {
            if (!TryGetActionData(actionId, out var action))
                return;

            if (action.Container == provider)
                RemoveAction(performer, actionId, comp);
        }
    }

    public void RemoveAction(EntityUid performer, EntityUid? actionId, ActionsComponent? comp = null, BaseActionComponent? action = null)
    {
        if (actionId == null)
            return;

        if (!ResolveActionData(actionId, ref action))
            return;

        if (!Resolve(performer, ref comp, false))
        {
            DebugTools.AssertNull(action.AttachedEntity);
            return;
        }

        if (action.AttachedEntity == null)
        {
            // action was already removed?
            DebugTools.Assert(!comp.Actions.Contains(actionId.Value));
            return;
        }


        DebugTools.Assert(action.AttachedEntity == performer);
        comp.Actions.Remove(actionId.Value);
        action.AttachedEntity = null;
        Dirty(actionId.Value, action);
        Dirty(performer, comp);

        ActionRemoved(performer, actionId.Value, comp, action);
    }

    protected virtual void ActionRemoved(EntityUid performer, EntityUid actionId, ActionsComponent comp, BaseActionComponent action)
    {
        // See client-side system for UI code.
    }
    #endregion

    #region EquipHandlers
    private void OnDidEquip(EntityUid uid, ActionsComponent component, DidEquipEvent args)
    {
        var ev = new GetItemActionsEvent(_actionContainer, args.Equipee, args.Equipment, args.SlotFlags);
        RaiseLocalEvent(args.Equipment, ev);

        if (ev.Actions.Count == 0)
            return;

        GrantActions(args.Equipee, ev.Actions, args.Equipment, component);
    }

    private void OnHandEquipped(EntityUid uid, ActionsComponent component, DidEquipHandEvent args)
    {
        var ev = new GetItemActionsEvent(_actionContainer, args.Equipped, args.User);
        RaiseLocalEvent(args.Equipped, ev);

        if (ev.Actions.Count == 0)
            return;

        GrantActions(args.User, ev.Actions, args.Equipped, component);
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

    public void SetEntityIcon(EntityUid uid, EntityUid? icon, BaseActionComponent? action = null)
    {
        if (!Resolve(uid, ref action))
            return;

        action.EntityIcon = icon;
        Dirty(uid, action);
    }
}
