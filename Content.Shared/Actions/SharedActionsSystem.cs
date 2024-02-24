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
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.Rejuvenate;

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

        SubscribeLocalEvent<InstantActionComponent, MapInitEvent>(OnActionMapInit);
        SubscribeLocalEvent<EntityTargetActionComponent, MapInitEvent>(OnActionMapInit);
        SubscribeLocalEvent<WorldTargetActionComponent, MapInitEvent>(OnActionMapInit);

        SubscribeLocalEvent<InstantActionComponent, ComponentShutdown>(OnActionShutdown);
        SubscribeLocalEvent<EntityTargetActionComponent, ComponentShutdown>(OnActionShutdown);
        SubscribeLocalEvent<WorldTargetActionComponent, ComponentShutdown>(OnActionShutdown);

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

        SubscribeLocalEvent<InstantActionComponent, GetActionDataEvent>(OnGetActionData);
        SubscribeLocalEvent<EntityTargetActionComponent, GetActionDataEvent>(OnGetActionData);
        SubscribeLocalEvent<WorldTargetActionComponent, GetActionDataEvent>(OnGetActionData);

        SubscribeAllEvent<RequestPerformActionEvent>(OnActionRequest);
    }

    private void OnActionMapInit(EntityUid uid, BaseActionComponent component, MapInitEvent args)
    {
        if (component.Charges == null)
            return;

        component.MaxCharges ??= component.Charges.Value;
        Dirty(uid, component);
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

        if (logError)
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
            DebugTools.AssertOwner(uid, result);
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

    public void SetCooldown(EntityUid? actionId, TimeSpan cooldown)
    {
        var start = GameTiming.CurTime;
        SetCooldown(actionId, start, start + cooldown);
    }

    public void ClearCooldown(EntityUid? actionId)
    {
        if (actionId == null)
            return;

        if (!TryGetActionData(actionId, out var action))
            return;

        if (action.Cooldown is not { } cooldown)
            return;

        action.Cooldown = (cooldown.Start, GameTiming.CurTime);
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
    protected virtual void UpdateAction(EntityUid? actionId, BaseActionComponent? action = null)
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

    public void SetCharges(EntityUid? actionId, int? charges)
    {
        if (!TryGetActionData(actionId, out var action) ||
            action.Charges == charges)
        {
            return;
        }

        action.Charges = charges;
        UpdateAction(actionId, action);
        Dirty(actionId.Value, action);
    }

    public int? GetCharges(EntityUid? actionId)
    {
        if (!TryGetActionData(actionId, out var action))
            return null;

        return action.Charges;
    }

    public void AddCharges(EntityUid? actionId, int addCharges)
    {
        if (!TryGetActionData(actionId, out var action) || action.Charges == null || addCharges < 1)
            return;

        action.Charges += addCharges;
        UpdateAction(actionId, action);
        Dirty(actionId.Value, action);
    }

    public void RemoveCharges(EntityUid? actionId, int? removeCharges)
    {
        if (!TryGetActionData(actionId, out var action) || action.Charges == null)
            return;

        if (removeCharges == null)
            action.Charges = removeCharges;
        else
            action.Charges -= removeCharges;

        if (action.Charges is < 0)
            action.Charges = null;

        UpdateAction(actionId, action);
        Dirty(actionId.Value, action);
    }

    public void ResetCharges(EntityUid? actionId)
    {
        if (!TryGetActionData(actionId, out var action))
            return;

        action.Charges = action.MaxCharges;
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
        // TODO: Check for charge recovery timer
        if (action.Cooldown.HasValue && action.Cooldown.Value.End > curTime)
            return;

        // TODO: Replace with individual charge recovery when we have the visuals to aid it
        if (action is { Charges: < 1, RenewCharges: true })
            ResetCharges(actionEnt);

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
                _rotateToFaceSystem.TryFaceCoordinates(user, entityCoordinatesTarget.ToMapPos(EntityManager, _transformSystem));

                if (!ValidateWorldTarget(user, entityCoordinatesTarget, worldAction))
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
            case InstantActionComponent instantAction:
                if (action.CheckCanInteract && !_actionBlockerSystem.CanInteract(user, null))
                    return;

                _adminLogger.Add(LogType.Action,
                    $"{ToPrettyString(user):user} is performing the {name:action} action provided by {ToPrettyString(action.Container ?? user):provider}.");

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

            if (!action.RaiseOnUser && action.Container != null && !HasComp<MindComponent>(action.Container))
                target = action.Container.Value;

            RaiseLocalEvent(target, (object) actionEvent, broadcast: true);
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
            if (action is { Charges: 0, RenewCharges: false })
                action.Enabled = false;
        }

        action.Cooldown = null;
        if (action is { UseDelay: not null, Charges: null or < 1 })
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
                Log.Error($"Attempted to remove an action {ToPrettyString(actionId)} from an entity that it was never attached to: {ToPrettyString(performer)}");
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

    #endregion

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
}
