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
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Actions;

public abstract class SharedActionsSystem : EntitySystem
{
    private const string ActionContainerId = "ActionContainer";
    private const string ProvidedActionContainerId = "ProvidedActionContainer";

    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly INetManager _net = default!;
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

        SubscribeLocalEvent<ActionsComponent, MapInitEvent>(OnActionsMapInit);
        SubscribeLocalEvent<ActionsComponent, ComponentGetState>(OnActionsGetState);
        SubscribeLocalEvent<ActionsComponent, ComponentShutdown>(OnActionsShutdown);

        SubscribeLocalEvent<InstantActionComponent, ComponentGetState>(OnInstantGetState);
        SubscribeLocalEvent<EntityTargetActionComponent, ComponentGetState>(OnEntityTargetGetState);
        SubscribeLocalEvent<WorldTargetActionComponent, ComponentGetState>(OnWorldTargetGetState);

        SubscribeLocalEvent<InstantActionComponent, ComponentHandleState>(OnInstantHandleState);
        SubscribeLocalEvent<EntityTargetActionComponent, ComponentHandleState>(OnEntityTargetHandleState);
        SubscribeLocalEvent<WorldTargetActionComponent, ComponentHandleState>(OnWorldTargetHandleState);

        SubscribeLocalEvent<InstantActionComponent, GetActionDataEvent>(OnGetActionData);
        SubscribeLocalEvent<EntityTargetActionComponent, GetActionDataEvent>(OnGetActionData);
        SubscribeLocalEvent<WorldTargetActionComponent, GetActionDataEvent>(OnGetActionData);

        SubscribeLocalEvent<InstantActionComponent, EntGotRemovedFromContainerMessage>(OnEntGotRemovedFromContainer);
        SubscribeLocalEvent<EntityTargetActionComponent, EntGotRemovedFromContainerMessage>(OnEntGotRemovedFromContainer);
        SubscribeLocalEvent<WorldTargetActionComponent, EntGotRemovedFromContainerMessage>(OnEntGotRemovedFromContainer);

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
        component.Provider = EnsureEntity<T>(state.Provider, uid);
        component.EntityIcon = EnsureEntity<T>(state.EntityIcon, uid);
        component.CheckCanInteract = state.CheckCanInteract;
        component.ClientExclusive = state.ClientExclusive;
        component.Priority = state.Priority;
        component.AttachedEntity = EnsureEntity<T>(state.AttachedEntity, uid);
        component.AutoPopulate = state.AutoPopulate;
        component.AutoRemove = state.AutoRemove;
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

    private void OnEntGotRemovedFromContainer<T>(EntityUid uid, T component, EntGotRemovedFromContainerMessage args) where T : BaseActionComponent
    {
        if (args.Container.ID != ProvidedActionContainerId)
            return;

        if (TryComp(component.AttachedEntity, out ActionsComponent? actions))
        {
            actions.Actions.Remove(uid);
            Dirty(component.AttachedEntity.Value, actions);

            if (TryGetActionData(uid, out var action))
                action.AttachedEntity = null;
        }
    }

    public BaseActionComponent? GetActionData(EntityUid? actionId)
    {
        if (actionId == null)
            return null;

        // TODO split up logic between each action component with different subscriptions
        // good luck future coder
        var ev = new GetActionDataEvent();
        RaiseLocalEvent(actionId.Value, ref ev);
        return ev.Action;
    }

    public bool TryGetActionData(
        [NotNullWhen(true)] EntityUid? actionId,
        [NotNullWhen(true)] out BaseActionComponent? action)
    {
        action = null;
        return actionId != null && (action = GetActionData(actionId)) != null;
    }

    protected Container EnsureContainer(EntityUid holderId, EntityUid? providerId)
    {
        return providerId == null
            ? _containerSystem.EnsureContainer<Container>(holderId, ActionContainerId)
            : _containerSystem.EnsureContainer<Container>(providerId.Value, ProvidedActionContainerId);
    }

    protected bool TryGetContainer(
        EntityUid holderId,
        [NotNullWhen(true)] out BaseContainer? container,
        ContainerManagerComponent? containerManager = null)
    {
        return _containerSystem.TryGetContainer(holderId, ActionContainerId, out container, containerManager);
    }

    protected bool TryGetProvidedContainer(
        EntityUid providerId,
        [NotNullWhen(true)] out BaseContainer? container,
        ContainerManagerComponent? containerManager = null)
    {
        return _containerSystem.TryGetContainer(providerId, ProvidedActionContainerId, out container, containerManager);
    }

    public void SetCooldown(EntityUid? actionId, TimeSpan start, TimeSpan end)
    {
        if (actionId == null)
            return;

        var action = GetActionData(actionId);
        if (action == null)
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

    private void OnActionsMapInit(EntityUid uid, ActionsComponent component, MapInitEvent args)
    {
        EnsureContainer(uid, null);
    }

    private void OnActionsGetState(EntityUid uid, ActionsComponent component, ref ComponentGetState args)
    {
        args.State = new ActionsComponentState(GetNetEntitySet(component.Actions));
    }

    private void OnActionsShutdown(EntityUid uid, ActionsComponent component, ComponentShutdown args)
    {
        if (TryGetContainer(uid, out var container))
            container.Shutdown(EntityManager);
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

        var action = GetActionData(actionEnt);
        if (action == null || !action.Enabled)
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

                if (action.Provider == null)
                {
                    _adminLogger.Add(LogType.Action,
                        $"{ToPrettyString(user):user} is performing the {name:action} action targeted at {ToPrettyString(entityTarget):target}.");
                }
                else
                {
                    _adminLogger.Add(LogType.Action,
                        $"{ToPrettyString(user):user} is performing the {name:action} action (provided by {ToPrettyString(action.Provider.Value):provider}) targeted at {ToPrettyString(entityTarget):target}.");
                }

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

                if (action.Provider == null)
                {
                    _adminLogger.Add(LogType.Action,
                        $"{ToPrettyString(user):user} is performing the {name:action} action targeted at {entityCoordinatesTarget:target}.");
                }
                else
                {
                    _adminLogger.Add(LogType.Action,
                        $"{ToPrettyString(user):user} is performing the {name:action} action (provided by {ToPrettyString(action.Provider.Value):provider}) targeted at {entityCoordinatesTarget:target}.");
                }

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

                if (action.Provider == null)
                {
                    _adminLogger.Add(LogType.Action,
                        $"{ToPrettyString(user):user} is performing the {name:action} action.");
                }
                else
                {
                    _adminLogger.Add(LogType.Action,
                        $"{ToPrettyString(user):user} is performing the {name:action} action provided by {ToPrettyString(action.Provider.Value):provider}.");
                }

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
            var provider = action.Provider;

            if (provider == null)
                RaiseLocalEvent(performer, (object) actionEvent, broadcast: true);
            else
                RaiseLocalEvent(provider.Value, (object) actionEvent, broadcast: true);

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
    /// <summary>
    ///     Add an action to an action holder.
    ///     If the holder has no actions component, this will give them one.
    /// </summary>
    public BaseActionComponent? AddAction(EntityUid holderId, ref EntityUid? actionId, string? actionPrototypeId, EntityUid? provider = null, ActionsComponent? holderComp = null)
    {
        if (Deleted(actionId))
        {
            if (_net.IsClient)
                return null;

            if (string.IsNullOrWhiteSpace(actionPrototypeId))
                return null;

            actionId = Spawn(actionPrototypeId);
        }

        AddAction(holderId, actionId.Value, provider, holderComp);
        return GetActionData(actionId);
    }

    /// <summary>
    ///     Add an action to an action holder.
    ///     If the holder has no actions component, this will give them one.
    /// </summary>
    /// <param name="holderId">Entity to receive the actions</param>
    /// <param name="actionId">Action entity to add</param>
    /// <param name="provider">The entity that enables these actions (e.g., flashlight). May be null (innate actions).</param>
    /// <param name="holder">Component of <see cref="holderId"/></param>
    /// <param name="action">Component of <see cref="actionId"/></param>
    /// <param name="actionContainer">Action container of <see cref="holderId"/></param>
    public virtual void AddAction(EntityUid holderId, EntityUid actionId, EntityUid? provider, ActionsComponent? holder = null, BaseActionComponent? action = null, bool dirty = true, BaseContainer? actionContainer = null)
    {
        action ??= GetActionData(actionId);
        // TODO remove when action subscriptions are split up
        if (action == null)
        {
            Log.Warning($"No {nameof(BaseActionComponent)} found on entity {actionId}");
            return;
        }

        holder ??= EnsureComp<ActionsComponent>(holderId);
        action.Provider = provider;
        action.AttachedEntity = holderId;
        Dirty(actionId, action);

        actionContainer ??= EnsureContainer(holderId, provider);
        AddActionInternal(holderId, actionId, actionContainer, holder);

        if (dirty)
            Dirty(holderId, holder);
    }

    protected virtual void AddActionInternal(EntityUid holderId, EntityUid actionId, BaseContainer container, ActionsComponent holder)
    {
        container.Insert(actionId);
        holder.Actions.Add(actionId);
        Dirty(holderId, holder);
    }

    /// <summary>
    ///     Add actions to an action component. If the entity has no action component, this will give them one.
    /// </summary>
    /// <param name="holderId">Entity to receive the actions</param>
    /// <param name="actions">The actions to add</param>
    /// <param name="provider">The entity that enables these actions (e.g., flashlight). May be null (innate actions).</param>
    public void AddActions(EntityUid holderId, IEnumerable<EntityUid> actions, EntityUid? provider, ActionsComponent? comp = null, bool dirty = true)
    {
        comp ??= EnsureComp<ActionsComponent>(holderId);

        var allClientExclusive = true;
        var container = EnsureContainer(holderId, provider);

        foreach (var actionId in actions)
        {
            var action = GetActionData(actionId);
            if (action == null)
                continue;

            AddAction(holderId, actionId, provider, comp, action, false, container);
            allClientExclusive = allClientExclusive && action.ClientExclusive;
        }

        if (dirty && !allClientExclusive)
            Dirty(holderId, comp);
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
    public void RemoveProvidedActions(EntityUid holderId, EntityUid provider, ActionsComponent? comp = null)
    {
        if (!Resolve(holderId, ref comp, false))
            return;

        if (!TryGetProvidedContainer(provider, out var container))
            return;

        foreach (var actionId in container.ContainedEntities.ToArray())
        {
            var action = GetActionData(actionId);
            if (action?.Provider == provider)
                RemoveAction(holderId, actionId, comp, dirty: false);
        }

        Dirty(holderId, comp);
    }

    public virtual void RemoveAction(EntityUid holderId, EntityUid? actionId, ActionsComponent? comp = null, BaseActionComponent? action = null, bool dirty = true)
    {
        if (actionId == null ||
            !Resolve(holderId, ref comp, false) ||
            TerminatingOrDeleted(actionId.Value))
        {
            return;
        }

        action ??= GetActionData(actionId);

        if (TryGetContainer(holderId, out var container) && container.Contains(actionId.Value))
            QueueDel(actionId.Value);

        comp.Actions.Remove(actionId.Value);

        if (action != null)
        {
            action.AttachedEntity = null;
            Dirty(actionId.Value, action);
        }

        if (dirty)
            Dirty(holderId, comp);

        DebugTools.Assert(Transform(actionId.Value).ParentUid.IsValid());
    }

    /// <summary>
    ///     Removes all actions with the given prototype id.
    /// </summary>
    public void RemoveAction(EntityUid holderId, string actionPrototypeId, ActionsComponent? holderComp = null)
    {
        if (!Resolve(holderId, ref holderComp, false))
            return;

        var actions = new List<(EntityUid Id, BaseActionComponent Comp)>();
        foreach (var (id, comp) in GetActions(holderId))
        {
            if (Prototype(id)?.ID == actionPrototypeId)
                actions.Add((id, comp));
        }

        if (actions.Count == 0)
            return;

        foreach (var action in actions)
        {
            RemoveAction(holderId, action.Id, holderComp, action.Comp);
        }
    }

    #endregion

    #region EquipHandlers
    private void OnDidEquip(EntityUid uid, ActionsComponent component, DidEquipEvent args)
    {
        var ev = new GetItemActionsEvent(EntityManager, _net, args.Equipee, args.SlotFlags);
        RaiseLocalEvent(args.Equipment, ev);

        if (ev.Actions.Count == 0)
            return;

        AddActions(args.Equipee, ev.Actions, args.Equipment, component);
    }

    private void OnHandEquipped(EntityUid uid, ActionsComponent component, DidEquipHandEvent args)
    {
        var ev = new GetItemActionsEvent(EntityManager, _net, args.User);
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
