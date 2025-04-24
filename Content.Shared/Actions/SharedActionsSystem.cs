using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Mind;
using Content.Shared.Rejuvenate;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Actions;

public abstract class SharedActionsSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private   readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private   readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private   readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private   readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private   readonly RotateToFaceSystem _rotateToFaceSystem = default!;
    [Dependency] private   readonly SharedAudioSystem _audio = default!;
    [Dependency] private   readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private   readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InstantActionComponent, MapInitEvent>(OnActionMapInit);
        SubscribeLocalEvent<EntityTargetActionComponent, MapInitEvent>(OnActionMapInit);
        SubscribeLocalEvent<WorldTargetActionComponent, MapInitEvent>(OnActionMapInit);
        SubscribeLocalEvent<EntityWorldTargetActionComponent, MapInitEvent>(OnActionMapInit);

        SubscribeLocalEvent<InstantActionComponent, ComponentShutdown>(OnActionShutdown);
        SubscribeLocalEvent<EntityTargetActionComponent, ComponentShutdown>(OnActionShutdown);
        SubscribeLocalEvent<WorldTargetActionComponent, ComponentShutdown>(OnActionShutdown);
        SubscribeLocalEvent<EntityWorldTargetActionComponent, ComponentShutdown>(OnActionShutdown);

        SubscribeLocalEvent<ActionsComponent, ActionComponentChangeEvent>(OnActionCompChange);
        SubscribeLocalEvent<ActionsComponent, RelayedActionComponentChangeEvent>(OnRelayActionCompChange);
        SubscribeLocalEvent<ActionsComponent, DidEquipEvent>(OnDidEquip);
        SubscribeLocalEvent<ActionsComponent, DidEquipHandEvent>(OnHandEquipped);
        SubscribeLocalEvent<ActionsComponent, DidUnequipEvent>(OnDidUnequip);
        SubscribeLocalEvent<ActionsComponent, DidUnequipHandEvent>(OnHandUnequipped);
        SubscribeLocalEvent<ActionsComponent, RejuvenateEvent>(OnRejuventate);

        SubscribeLocalEvent<ActionsComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ActionsComponent, ComponentGetState>(OnActionsGetState);

        SubscribeLocalEvent<InstantActionComponent, ComponentGetState>(OnInstantGetState);
        SubscribeLocalEvent<EntityTargetActionComponent, ComponentGetState>(OnEntityTargetGetState);
        SubscribeLocalEvent<WorldTargetActionComponent, ComponentGetState>(OnWorldTargetGetState);
        SubscribeLocalEvent<EntityWorldTargetActionComponent, ComponentGetState>(OnEntityWorldTargetGetState);

        SubscribeLocalEvent<InstantActionComponent, GetActionDataEvent>(OnGetActionData);
        SubscribeLocalEvent<EntityTargetActionComponent, GetActionDataEvent>(OnGetActionData);
        SubscribeLocalEvent<WorldTargetActionComponent, GetActionDataEvent>(OnGetActionData);
        SubscribeLocalEvent<EntityWorldTargetActionComponent, GetActionDataEvent>(OnGetActionData);

        SubscribeAllEvent<RequestPerformActionEvent>(OnActionRequest);
    }

    private void OnActionMapInit(EntityUid uid, BaseActionComponent component, MapInitEvent args)
    {
        component.OriginalIconColor = component.IconColor;
    }

    private void OnActionShutdown(EntityUid uid, BaseActionComponent component, ComponentShutdown args)
    {
        if (component.AttachedEntity != null && !TerminatingOrDeleted(component.AttachedEntity.Value))
            RemoveAction(component.AttachedEntity.Value, uid, action: component);
    }

    private void OnShutdown(EntityUid uid, ActionsComponent component, ComponentShutdown args)
    {
        foreach (var act in component.Actions)
        {
            RemoveAction(uid, act, component);
        }
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

    private void OnEntityWorldTargetGetState(EntityUid uid, EntityWorldTargetActionComponent component, ref ComponentGetState args)
    {
        args.State = new EntityWorldTargetActionComponentState(component, EntityManager);
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
        if (uid == null || TerminatingOrDeleted(uid.Value))
            return false;

        var ev = new GetActionDataEvent();
        RaiseLocalEvent(uid.Value, ref ev);
        result = ev.Action;

        if (result != null)
            return true;

        if (logError)
            Log.Error($"Failed to get action from action entity: {ToPrettyString(uid.Value)}. Trace: {Environment.StackTrace}");

        return false;
    }

    public bool ResolveActionData(
        [NotNullWhen(true)] EntityUid? uid,
        [NotNullWhen(true)] ref BaseActionComponent? result,
        bool logError = true)
    {
        if (result != null)
        {
            DebugTools.AssertOwner(uid, result);
            return true;
        }

        return TryGetActionData(uid, out result, logError);
    }

    public void SetCooldown(EntityUid? actionId, TimeSpan start, TimeSpan end)
    {
        if (!TryGetActionData(actionId, out var action))
            return;

        action.Cooldown = (start, end);
        Dirty(actionId.Value, action);
    }

    public void SetCooldown(EntityUid? actionId, TimeSpan cooldown)
    {
        var start = GameTiming.CurTime;
        SetCooldown(actionId, start, start + cooldown);
    }

    public void ClearCooldown(EntityUid? actionId)
    {
        if (!TryGetActionData(actionId, out var action))
            return;

        if (action.Cooldown is not { } cooldown)
            return;

        action.Cooldown = (cooldown.Start, GameTiming.CurTime);
        Dirty(actionId.Value, action);
    }

    /// <summary>
    ///     Sets the cooldown for this action only if it is bigger than the one it already has.
    /// </summary>
    public void SetIfBiggerCooldown(EntityUid? actionId, TimeSpan? cooldown)
    {
        if (cooldown == null ||
            cooldown.Value <= TimeSpan.Zero ||
            !TryGetActionData(actionId, out var action))
        {
            return;
        }

        var start = GameTiming.CurTime;
        var end = start + cooldown;
        if (action.Cooldown?.End > end)
            return;

        action.Cooldown = (start, end.Value);
        Dirty(actionId.Value, action);
    }

    public void StartUseDelay(EntityUid? actionId)
    {
        if (actionId == null)
            return;

        if (!TryGetActionData(actionId, out var action) || action.UseDelay == null)
            return;

        action.Cooldown = (GameTiming.CurTime, GameTiming.CurTime + action.UseDelay.Value);
        Dirty(actionId.Value, action);
    }

    public void SetUseDelay(EntityUid? actionId, TimeSpan? delay)
    {
        if (!TryGetActionData(actionId, out var action) || action.UseDelay == delay)
            return;

        action.UseDelay = delay;
        UpdateAction(actionId, action);
        Dirty(actionId.Value, action);
    }

    public void ReduceUseDelay(EntityUid? actionId, TimeSpan? lowerDelay)
    {
        if (!TryGetActionData(actionId, out var action))
            return;

        if (action.UseDelay != null && lowerDelay != null)
            action.UseDelay = action.UseDelay - lowerDelay;

        if (action.UseDelay < TimeSpan.Zero)
            action.UseDelay = null;

        UpdateAction(actionId, action);
        Dirty(actionId.Value, action);
    }

    private void OnRejuventate(EntityUid uid, ActionsComponent component, RejuvenateEvent args)
    {
        foreach (var act in component.Actions)
        {
            ClearCooldown(act);
        }
    }

    #region ComponentStateManagement
    public virtual void UpdateAction(EntityUid? actionId, BaseActionComponent? action = null)
    {
        // See client-side code.
    }

    public void SetToggled(EntityUid? actionId, bool toggled)
    {
        if (!TryGetActionData(actionId, out var action) ||
            action.Toggled == toggled)
        {
            return;
        }

        action.Toggled = toggled;
        UpdateAction(actionId, action);
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
        UpdateAction(actionId, action);
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

        DebugTools.Assert(action.AttachedEntity == user);
        if (!action.Enabled)
            return;

        var curTime = GameTiming.CurTime;
        if (IsCooldownActive(action, curTime))
            return;

        // check for action use prevention
        // TODO: make code below use this event with a dedicated component
        var attemptEv = new ActionAttemptEvent(user);
        RaiseLocalEvent(actionEnt, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        BaseActionEvent? performEvent = null;

        if (action.CheckConsciousness && !_actionBlockerSystem.CanConsciouslyPerformAction(user))
            return;

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

                if (!ValidateEntityTarget(user, entityTarget, (actionEnt, entityAction)))
                    return;

                _adminLogger.Add(LogType.Action,
                    $"{ToPrettyString(user):user} is performing the {name:action} action (provided by {ToPrettyString(action.Container ?? user):provider}) targeted at {ToPrettyString(entityTarget):target}.");

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
                _rotateToFaceSystem.TryFaceCoordinates(user, _transformSystem.ToMapCoordinates(entityCoordinatesTarget).Position);

                if (!ValidateWorldTarget(user, entityCoordinatesTarget, (actionEnt, worldAction)))
                    return;

                _adminLogger.Add(LogType.Action,
                    $"{ToPrettyString(user):user} is performing the {name:action} action (provided by {ToPrettyString(action.Container ?? user):provider}) targeted at {entityCoordinatesTarget:target}.");

                if (worldAction.Event != null)
                {
                    worldAction.Event.Target = entityCoordinatesTarget;
                    Dirty(actionEnt, worldAction);
                    performEvent = worldAction.Event;
                }

                break;
            case EntityWorldTargetActionComponent entityWorldAction:
            {
                var actionEntity = GetEntity(ev.EntityTarget);
                var actionCoords = GetCoordinates(ev.EntityCoordinatesTarget);

                if (actionEntity is null && actionCoords is null)
                {
                    Log.Error($"Attempted to perform an entity-world-targeted action without an entity or world coordinates! Action: {name}");
                    return;
                }

                var entWorldAction = new Entity<EntityWorldTargetActionComponent>(actionEnt, entityWorldAction);

                if (!ValidateEntityWorldTarget(user, actionEntity, actionCoords, entWorldAction))
                    return;

                _adminLogger.Add(LogType.Action,
                    $"{ToPrettyString(user):user} is performing the {name:action} action (provided by {ToPrettyString(action.Container ?? user):provider}) targeted at {ToPrettyString(actionEntity):target} {actionCoords:target}.");

                if (entityWorldAction.Event != null)
                {
                    entityWorldAction.Event.Entity = actionEntity;
                    entityWorldAction.Event.Coords = actionCoords;
                    Dirty(actionEnt, entityWorldAction);
                    performEvent = entityWorldAction.Event;
                }
                break;
            }
            case InstantActionComponent instantAction:
                if (action.CheckCanInteract && !_actionBlockerSystem.CanInteract(user, null))
                    return;

                _adminLogger.Add(LogType.Action,
                    $"{ToPrettyString(user):user} is performing the {name:action} action provided by {ToPrettyString(action.Container ?? user):provider}.");

                performEvent = instantAction.Event;
                break;
        }

        // All checks passed. Perform the action!
        PerformAction(user, component, actionEnt, action, performEvent, curTime);
    }

    public bool ValidateEntityTarget(EntityUid user, EntityUid target, Entity<EntityTargetActionComponent> actionEnt)
    {
        var comp = actionEnt.Comp;
        if (!ValidateEntityTargetBase(user,
                target,
                comp.Whitelist,
                comp.Blacklist,
                comp.CheckCanInteract,
                comp.CanTargetSelf,
                comp.CheckCanAccess,
                comp.Range))
            return false;

        var ev = new ValidateActionEntityTargetEvent(user, target);
        RaiseLocalEvent(actionEnt, ref ev);
        return !ev.Cancelled;
    }

    private bool ValidateEntityTargetBase(EntityUid user,
        EntityUid? targetEntity,
        EntityWhitelist? whitelist,
        EntityWhitelist? blacklist,
        bool checkCanInteract,
        bool canTargetSelf,
        bool checkCanAccess,
        float range)
    {
        if (targetEntity is not { } target || !target.IsValid() || Deleted(target))
            return false;

        if (_whitelistSystem.IsWhitelistFail(whitelist, target))
            return false;

        if (_whitelistSystem.IsBlacklistPass(blacklist, target))
            return false;

        if (checkCanInteract && !_actionBlockerSystem.CanInteract(user, target))
            return false;

        if (user == target)
            return canTargetSelf;

        if (!checkCanAccess)
        {
            // even if we don't check for obstructions, we may still need to check the range.
            var xform = Transform(user);
            var targetXform = Transform(target);

            if (xform.MapID != targetXform.MapID)
                return false;

            if (range <= 0)
                return true;

            var distance = (_transformSystem.GetWorldPosition(xform) - _transformSystem.GetWorldPosition(targetXform)).Length();
            return distance <= range;
        }

        return _interactionSystem.InRangeAndAccessible(user, target, range: range);
    }

    public bool ValidateWorldTarget(EntityUid user, EntityCoordinates coords, Entity<WorldTargetActionComponent> action)
    {
        var comp = action.Comp;
        if (!ValidateWorldTargetBase(user, coords, comp.CheckCanInteract, comp.CheckCanAccess, comp.Range))
            return false;

        var ev = new ValidateActionWorldTargetEvent(user, coords);
        RaiseLocalEvent(action, ref ev);
        return !ev.Cancelled;
    }

    private bool ValidateWorldTargetBase(EntityUid user,
        EntityCoordinates? entityCoordinates,
        bool checkCanInteract,
        bool checkCanAccess,
        float range)
    {
        if (entityCoordinates is not { } coords)
            return false;

        if (checkCanInteract && !_actionBlockerSystem.CanInteract(user, null))
            return false;

        if (!checkCanAccess)
        {
            // even if we don't check for obstructions, we may still need to check the range.
            var xform = Transform(user);

            if (xform.MapID != _transformSystem.GetMapId(coords))
                return false;

            if (range <= 0)
                return true;
            return _transformSystem.InRange(coords, xform.Coordinates, range);
        }

        return _interactionSystem.InRangeUnobstructed(user, coords, range: range);
    }

    public bool ValidateEntityWorldTarget(EntityUid user,
        EntityUid? entity,
        EntityCoordinates? coords,
        Entity<EntityWorldTargetActionComponent> action)
    {
        var comp = action.Comp;
        var entityValidated = ValidateEntityTargetBase(user,
            entity,
            comp.Whitelist,
            null,
            comp.CheckCanInteract,
            comp.CanTargetSelf,
            comp.CheckCanAccess,
            comp.Range);

        var worldValidated
            = ValidateWorldTargetBase(user, coords, comp.CheckCanInteract, comp.CheckCanAccess, comp.Range);

        if (!entityValidated && !worldValidated)
            return false;

        var ev = new ValidateActionEntityWorldTargetEvent(user,
            entityValidated ? entity : null,
            worldValidated ? coords : null);
        RaiseLocalEvent(action, ref ev);
        return !ev.Cancelled;
    }

    public void PerformAction(EntityUid performer, ActionsComponent? component, EntityUid actionId, BaseActionComponent action, BaseActionEvent? actionEvent, TimeSpan curTime, bool predicted = true)
    {
        var handled = false;

        var toggledBefore = action.Toggled;

        // Note that attached entity and attached container are allowed to be null here.
        if (action.AttachedEntity != null && action.AttachedEntity != performer)
        {
            Log.Error($"{ToPrettyString(performer)} is attempting to perform an action {ToPrettyString(actionId)} that is attached to another entity {ToPrettyString(action.AttachedEntity.Value)}");
            return;
        }

        if (actionEvent != null)
        {
            // This here is required because of client-side prediction (RaisePredictiveEvent results in event re-use).
            actionEvent.Handled = false;
            var target = performer;
            actionEvent.Performer = performer;
            actionEvent.Action = (actionId, action);

            if (!action.RaiseOnUser && action.Container != null && !HasComp<MindComponent>(action.Container))
                target = action.Container.Value;

            if (action.RaiseOnAction)
                target = actionId;

            RaiseLocalEvent(target, (object) actionEvent, broadcast: true);
            handled = actionEvent.Handled;
        }

        if (!handled)
            return; // no interaction occurred.

        // play sound, reduce charges, start cooldown, and mark as dirty (if required).
        if (actionEvent?.Toggle == true)
        {
            action.Toggled = !action.Toggled;
        }

        _audio.PlayPredicted(action.Sound, performer, predicted ? performer : null);

        var dirty = toggledBefore != action.Toggled;

        action.Cooldown = null;
        if (action is { UseDelay: not null})
        {
            dirty = true;
            action.Cooldown = (curTime, curTime + action.UseDelay.Value);
        }

        if (dirty)
        {
            Dirty(actionId, action);
            UpdateAction(actionId, action);
        }

        var ev = new ActionPerformedEvent(performer);
        RaiseLocalEvent(actionId, ref ev);
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
    /// <param name="performer">Entity to receive the actions</param>
    /// <param name="actionId">Action entity to add</param>
    /// <param name="component">The <see cref="performer"/>'s action component of </param>
    /// <param name="actionPrototypeId">The action entity prototype id to use if <see cref="actionId"/> is invalid.</param>
    /// <param name="container">The entity that contains/enables this action (e.g., flashlight).</param>
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

        return AddActionDirect(performer, actionId.Value, component, action);
    }

    /// <summary>
    ///     Adds a pre-existing action.
    /// </summary>
    public bool AddAction(EntityUid performer,
        EntityUid actionId,
        EntityUid container,
        ActionsComponent? comp = null,
        BaseActionComponent? action = null,
        ActionsContainerComponent? containerComp = null
        )
    {
        if (!ResolveActionData(actionId, ref action))
            return false;

        if (action.Container != container
            || !Resolve(container, ref containerComp)
            || !containerComp.Container.Contains(actionId))
        {
            Log.Error($"Attempted to add an action with an invalid container: {ToPrettyString(actionId)}");
            return false;
        }

        return AddActionDirect(performer, actionId, comp, action);
    }

    /// <summary>
    ///     Adds a pre-existing action. This also bypasses the requirement that the given action must be stored in a
    ///     valid action container.
    /// </summary>
    public bool AddActionDirect(EntityUid performer,
        EntityUid actionId,
        ActionsComponent? comp = null,
        BaseActionComponent? action = null)
    {
        if (!ResolveActionData(actionId, ref action))
            return false;

        DebugTools.Assert(action.Container == null ||
                          (TryComp(action.Container, out ActionsContainerComponent? containerComp)
                           && containerComp.Container.Contains(actionId)));

        if (action.AttachedEntity != null)
            RemoveAction(action.AttachedEntity.Value, actionId, action: action);

        if (action.StartDelay && action.UseDelay != null)
            SetCooldown(actionId, action.UseDelay.Value);

        DebugTools.AssertOwner(performer, comp);
        comp ??= EnsureComp<ActionsComponent>(performer);
        action.AttachedEntity = performer;
        comp.Actions.Add(actionId);
        Dirty(actionId, action);
        Dirty(performer, comp);
        ActionAdded(performer, actionId, comp, action);
        return true;
    }

    /// <summary>
    /// This method gets called after a new action got added.
    /// </summary>
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
    public void GrantActions(EntityUid performer, IEnumerable<EntityUid> actions, EntityUid container, ActionsComponent? comp = null, ActionsContainerComponent? containerComp = null)
    {
        if (!Resolve(container, ref containerComp))
            return;

        DebugTools.AssertOwner(performer, comp);
        comp ??= EnsureComp<ActionsComponent>(performer);

        foreach (var actionId in actions)
        {
            AddAction(performer, actionId, container, comp, containerComp: containerComp);
        }
    }

    /// <summary>
    ///     Grants all actions currently contained in some action-container. If the target entity has no action
    /// component, this will give them one.
    /// </summary>
    /// <param name="performer">Entity to receive the actions</param>
    /// <param name="container">The entity that contains thee actions.</param>
    public void GrantContainedActions(Entity<ActionsComponent?> performer, Entity<ActionsContainerComponent?> container)
    {
        if (!Resolve(container, ref container.Comp))
            return;

        performer.Comp ??= EnsureComp<ActionsComponent>(performer);

        foreach (var actionId in container.Comp.Container.ContainedEntities)
        {
            if (TryGetActionData(actionId, out var action))
                AddActionDirect(performer, actionId, performer.Comp, action);
        }
    }

    /// <summary>
    ///     Grants the provided action from the container to the target entity. If the target entity has no action
    /// component, this will give them one.
    /// </summary>
    /// <param name="performer"></param>
    /// <param name="container"></param>
    /// <param name="actionId"></param>
    public void GrantContainedAction(Entity<ActionsComponent?> performer, Entity<ActionsContainerComponent?> container, EntityUid actionId)
    {
        if (!Resolve(container, ref container.Comp))
            return;

        performer.Comp ??= EnsureComp<ActionsComponent>(performer);

        if (TryGetActionData(actionId, out var action))
            AddActionDirect(performer, actionId, performer.Comp, action);
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
    public void RemoveProvidedActions(EntityUid performer, EntityUid container, ActionsComponent? comp = null)
    {
        if (!Resolve(performer, ref comp, false))
            return;

        foreach (var actionId in comp.Actions.ToArray())
        {
            if (!TryGetActionData(actionId, out var action))
                return;

            if (action.Container == container)
                RemoveAction(performer, actionId, comp);
        }
    }

    /// <summary>
    ///     Removes a single provided action provided by another entity.
    /// </summary>
    public void RemoveProvidedAction(EntityUid performer, EntityUid container, EntityUid actionId, ActionsComponent? comp = null)
    {
        if (!Resolve(performer, ref comp, false) || !TryGetActionData(actionId, out var action))
            return;

        if (action.Container == container)
            RemoveAction(performer, actionId, comp);
    }

    public void RemoveAction(EntityUid? actionId)
    {
        if (actionId == null)
            return;

        if (!TryGetActionData(actionId, out var action))
            return;

        if (!TryComp(action.AttachedEntity, out ActionsComponent? comp))
            return;

        RemoveAction(action.AttachedEntity.Value, actionId, comp, action);
    }

    public void RemoveAction(EntityUid performer, EntityUid? actionId, ActionsComponent? comp = null, BaseActionComponent? action = null)
    {
        if (actionId == null)
            return;

        if (!ResolveActionData(actionId, ref action))
            return;

        if (action.AttachedEntity != performer)
        {
            DebugTools.Assert(!Resolve(performer, ref comp, false)
                              || comp.LifeStage >= ComponentLifeStage.Stopping
                              || !comp.Actions.Contains(actionId.Value));

            if (!GameTiming.ApplyingState)
                Log.Error($"Attempted to remove an action {ToPrettyString(actionId)} from an entity that it was never attached to: {ToPrettyString(performer)}. Trace: {Environment.StackTrace}");
            return;
        }

        if (!Resolve(performer, ref comp, false))
        {
            DebugTools.Assert(action.AttachedEntity == null || TerminatingOrDeleted(action.AttachedEntity.Value));
            action.AttachedEntity = null;
            return;
        }

        if (action.AttachedEntity == null)
        {
            // action was already removed?
            DebugTools.Assert(!comp.Actions.Contains(actionId.Value) || GameTiming.ApplyingState);
            return;
        }

        comp.Actions.Remove(actionId.Value);
        action.AttachedEntity = null;
        Dirty(actionId.Value, action);
        Dirty(performer, comp);
        ActionRemoved(performer, actionId.Value, comp, action);
        if (action.Temporary)
            QueueDel(actionId.Value);
    }

    /// <summary>
    /// This method gets called after an action got removed.
    /// </summary>
    protected virtual void ActionRemoved(EntityUid performer, EntityUid actionId, ActionsComponent comp, BaseActionComponent action)
    {
        // See client-side system for UI code.
    }

    public bool ValidAction(BaseActionComponent action, bool canReach = true)
    {
        if (!action.Enabled)
            return false;


        var curTime = GameTiming.CurTime;
        if (action.Cooldown.HasValue && action.Cooldown.Value.End > curTime)
            return false;

        return canReach || action is BaseTargetActionComponent { CheckCanAccess: false };
    }

    #endregion

    private void OnRelayActionCompChange(Entity<ActionsComponent> ent, ref RelayedActionComponentChangeEvent args)
    {
        if (args.Handled)
            return;

        var ev = new AttemptRelayActionComponentChangeEvent();
        RaiseLocalEvent(ent.Owner, ref ev);
        var target = ev.Target ?? ent.Owner;

        args.Handled = true;
        args.Toggle = true;

        if (!args.Action.Comp.Toggled)
        {
            EntityManager.AddComponents(target, args.Components);
        }
        else
        {
            EntityManager.RemoveComponents(target, args.Components);
        }
    }

    private void OnActionCompChange(Entity<ActionsComponent> ent, ref ActionComponentChangeEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.Toggle = true;
        var target = ent.Owner;

        if (!args.Action.Comp.Toggled)
        {
            EntityManager.AddComponents(target, args.Components);
        }
        else
        {
            EntityManager.RemoveComponents(target, args.Components);
        }
    }

    #region EquipHandlers
    private void OnDidEquip(EntityUid uid, ActionsComponent component, DidEquipEvent args)
    {
        if (GameTiming.ApplyingState)
            return;

        var ev = new GetItemActionsEvent(_actionContainer, args.Equipee, args.Equipment, args.SlotFlags);
        RaiseLocalEvent(args.Equipment, ev);

        if (ev.Actions.Count == 0)
            return;

        GrantActions(args.Equipee, ev.Actions, args.Equipment, component);
    }

    private void OnHandEquipped(EntityUid uid, ActionsComponent component, DidEquipHandEvent args)
    {
        if (GameTiming.ApplyingState)
            return;

        var ev = new GetItemActionsEvent(_actionContainer, args.User, args.Equipped);
        RaiseLocalEvent(args.Equipped, ev);

        if (ev.Actions.Count == 0)
            return;

        GrantActions(args.User, ev.Actions, args.Equipped, component);
    }

    private void OnDidUnequip(EntityUid uid, ActionsComponent component, DidUnequipEvent args)
    {
        if (GameTiming.ApplyingState)
            return;

        RemoveProvidedActions(uid, args.Equipment, component);
    }

    private void OnHandUnequipped(EntityUid uid, ActionsComponent component, DidUnequipHandEvent args)
    {
        if (GameTiming.ApplyingState)
            return;

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

    /// <summary>
    ///     Checks if the action has a cooldown and if it's still active
    /// </summary>
    public bool IsCooldownActive(BaseActionComponent action, TimeSpan? curTime = null)
    {
        // TODO: Check for charge recovery timer
        return action.Cooldown.HasValue && action.Cooldown.Value.End > curTime;
    }
}
