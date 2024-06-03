using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions.Components;
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
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Actions;

public abstract class SharedActionsSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly RotateToFaceSystem _rotateToFace = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<ActionComponent> _actionQuery;
    private EntityQuery<ActionsComponent> _actionsQuery;
    private EntityQuery<MindComponent> _mindQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _actionQuery = GetEntityQuery<ActionComponent>();
        _actionsQuery = GetEntityQuery<ActionsComponent>();
        _mindQuery = GetEntityQuery<MindComponent>();

        SubscribeLocalEvent<ActionComponent, MapInitEvent>(OnActionMapInit);

        SubscribeLocalEvent<ActionComponent, ComponentShutdown>(OnActionShutdown);

        SubscribeLocalEvent<ActionsComponent, DidEquipEvent>(OnDidEquip);
        SubscribeLocalEvent<ActionsComponent, DidEquipHandEvent>(OnHandEquipped);
        SubscribeLocalEvent<ActionsComponent, DidUnequipEvent>(OnDidUnequip);
        SubscribeLocalEvent<ActionsComponent, DidUnequipHandEvent>(OnHandUnequipped);
        SubscribeLocalEvent<ActionsComponent, RejuvenateEvent>(OnRejuventate);

        SubscribeLocalEvent<ActionsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ActionsComponent, ComponentGetState>(OnGetState);

        SubscribeLocalEvent<InstantActionComponent, ActionValidateEvent>(OnInstantValidate);
        SubscribeLocalEvent<EntityTargetActionComponent, ActionValidateEvent>(OnEntityValidate);
        SubscribeLocalEvent<WorldTargetActionComponent, ActionValidateEvent>(OnWorldValidate);

        SubscribeLocalEvent<InstantActionComponent, ActionSetEventEvent>(OnInstantSetEvent);
        SubscribeLocalEvent<EntityTargetActionComponent, ActionSetEventEvent>(OnEntitySetEvent);
        SubscribeLocalEvent<WorldTargetActionComponent, ActionSetEventEvent>(OnWorldSetEvent);

        SubscribeAllEvent<RequestPerformActionEvent>(OnActionRequest);
    }

    private void OnActionMapInit(Entity<ActionComponent> ent, ref MapInitEvent args)
    {
        var (uid, comp) = ent;
        if (comp.Charges == comp.MaxCharges || comp.Charges is not {} charges)
            return;

        comp.MaxCharges ??= charges;
        Dirty(uid, comp);
    }

    private void OnActionShutdown(Entity<ActionComponent> ent, ref ComponentShutdown args)
    {
        var (uid, comp) = ent;
        if (comp.AttachedEntity is {} user && !TerminatingOrDeleted(user))
            RemoveAction(user, uid, action: comp);
    }

    private void OnShutdown(Entity<ActionsComponent> ent, ref ComponentShutdown args)
    {
        var (uid, comp) = ent;
        foreach (var act in comp.Actions)
        {
            RemoveAction(uid, act, comp);
        }
    }

    private void OnGetState(Entity<ActionsComponent> ent, ref ComponentGetState args)
    {
        args.State = new ActionsComponentState(GetNetEntitySet(ent.Comp.Actions));
    }

    public bool TryGetActionData(
        [NotNullWhen(true)] EntityUid? uid,
        [NotNullWhen(true)] out ActionComponent? result,
        bool logError = true)
    {
        result = null;
        if (uid == null || TerminatingOrDeleted(uid.Value))
            return false;

        if (_actionQuery.TryComp(uid, out result))
            return true;

        if (logError)
            Log.Error($"Failed to get action from action entity: {ToPrettyString(uid.Value)}. Trace: {Environment.StackTrace}");

        return false;
    }

    public bool ResolveActionData(
        [NotNullWhen(true)] EntityUid? uid,
        [NotNullWhen(true)] ref ActionComponent? result,
        bool logError = true)
    {
        return TryGetActionData(uid, out result, logError);
    }

    public void SetCooldown(EntityUid? actionId, TimeSpan start, TimeSpan end)
    {
        if (actionId is not {} uid)
            return;

        if (!TryGetActionData(uid, out var action))
            return;

        action.Cooldown = new ActionCooldown
        {
            Start = start,
            End = end
        };
        Dirty(uid, action);
    }

    public void SetCooldown(EntityUid? actionId, TimeSpan cooldown)
    {
        var start = GameTiming.CurTime;
        SetCooldown(actionId, start, start + cooldown);
    }

    public void ClearCooldown(EntityUid? actionId)
    {
        if (actionId is not {} uid)
            return;

        if (!TryGetActionData(uid, out var action))
            return;

        if (action.Cooldown is not {} cooldown)
            return;

        action.Cooldown = new ActionCooldown
        {
            Start = cooldown.Start,
            End = GameTiming.CurTime
        };
        Dirty(uid, action);
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

        action.Cooldown = new ActionCooldown
        {
            Start = start,
            End = end.Value
        };
        Dirty(actionId.Value, action);
    }

    public void StartUseDelay(EntityUid? actionId)
    {
        if (actionId is not {} uid)
            return;

        if (!TryGetActionData(uid, out var action) || action.UseDelay is not {} delay)
            return;

        SetCooldown(uid, delay);
    }

    public void SetUseDelay(EntityUid? actionId, TimeSpan? delay)
    {
        if (actionId is not {} uid)
            return;

        if (!TryGetActionData(uid, out var action) || action.UseDelay == delay)
            return;

        action.UseDelay = delay;
        UpdateAction(uid, action);
        Dirty(uid, action);
    }

    public void ReduceUseDelay(EntityUid? actionId, TimeSpan? lowerDelay)
    {
        if (actionId is not {} uid)
            return;

        if (!TryGetActionData(uid, out var action))
            return;

        if (action.UseDelay != null && lowerDelay != null)
            action.UseDelay = action.UseDelay - lowerDelay;

        if (action.UseDelay < TimeSpan.Zero)
            action.UseDelay = null;

        UpdateAction(uid, action);
        Dirty(uid, action);
    }

    private void OnRejuventate(Entity<ActionsComponent> ent, ref RejuvenateEvent args)
    {
        foreach (var act in ent.Comp.Actions)
        {
            ClearCooldown(act);
        }
    }

    #region ComponentStateManagement
    protected virtual void UpdateAction(EntityUid? actionId, ActionComponent? action = null)
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

        if (action.Charges < 0)
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

        if (!_actionsQuery.TryComp(user, out var component))
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

        // check for action use prevention
        // TODO: make code below use this event with a dedicated component
        var attemptEv = new ActionAttemptEvent(user);
        RaiseLocalEvent(actionEnt, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        var curTime = GameTiming.CurTime;
        // TODO: Check for charge recovery timer
        if (action.Cooldown.HasValue && action.Cooldown.Value.End > curTime)
            return;

        // TODO: Replace with individual charge recovery when we have the visuals to aid it
        if (action is { Charges: < 1, RenewCharges: true })
            ResetCharges(actionEnt);

        if (action.CheckConsciousness && !_actionBlocker.CanConsciouslyPerformAction(user))
            return;

        if (action.CheckCanInteract && !_actionBlocker.CanInteract(user, null))
            return;

        // Validate request by checking action blockers and the like
        var provider = action.Container ?? user;
        var validateEv = new ActionValidateEvent()
        {
            Input = ev,
            User = user,
            Provider = provider
        };
        RaiseLocalEvent(actionEnt, ref validateEv);
        if (validateEv.Invalid)
        {
            Log.Error($"{ToPrettyString(user):user} attempted to perform an action {name} with invalid input!");
            return;
        }

        if (validateEv.Event is not {} performEvent)
            return;

        // All checks passed. Perform the action!
        PerformAction(user, component, actionEnt, action, performEvent, curTime);
    }

    private void OnInstantValidate(Entity<InstantActionComponent> ent, ref ActionValidateEvent args)
    {
        _adminLogger.Add(LogType.Action,
            $"{ToPrettyString(args.User):user} is performing the {Name(ent):action} action provided by {ToPrettyString(args.Provider):provider}.");

        args.Event = ent.Comp.Event;
    }

    private void OnEntityValidate(Entity<EntityTargetActionComponent> ent, ref ActionValidateEvent args)
    {
        if (args.Input.EntityTarget is not { Valid: true } netTarget)
        {
            args.Invalid = true;
            return;
        }

        var user = args.User;

        var target = GetEntity(netTarget);

        var targetWorldPos = _transform.GetWorldPosition(target);
        _rotateToFace.TryFaceCoordinates(user, targetWorldPos);

        if (!ValidateEntityTarget(user, target, ent))
            return;

        _adminLogger.Add(LogType.Action,
            $"{ToPrettyString(user):user} is performing the {Name(ent):action} action (provided by {ToPrettyString(args.Provider):provider}) targeted at {ToPrettyString(target):target}.");

        if (ent.Comp.Event is {} ev)
        {
            ev.Target = target;
            args.Event = ev;
        }
    }

    private void OnWorldValidate(Entity<WorldTargetActionComponent> ent, ref ActionValidateEvent args)
    {
        if (args.Input.EntityCoordinatesTarget is not { } netTarget)
        {
            args.Invalid = true;
            return;
        }

        var user = args.User;
        var target = GetCoordinates(netTarget);
        _rotateToFace.TryFaceCoordinates(user, target.ToMapPos(EntityManager, _transform));

        var targetAction = Comp<TargetActionComponent>(ent);
        if (!ValidateBaseTarget(user, target, (ent, targetAction)))
            return;

        _adminLogger.Add(LogType.Action,
            $"{ToPrettyString(user):user} is performing the {Name(ent):action} action (provided by {args.Provider}) targeted at {target:target}.");

        if (ent.Comp.Event is {} ev)
        {
            ev.Target = target;
            args.Event = ev;
        }
    }

    public bool ValidateEntityTarget(EntityUid user, EntityUid target, Entity<EntityTargetActionComponent> ent)
    {
        var (uid, comp) = ent;
        if (!target.IsValid() || Deleted(target))
            return false;

        if (_whitelist.IsWhitelistFail(comp.Whitelist, target))
            return false;

        if (_actionQuery.GetComponent(uid).CheckCanInteract && !_actionBlocker.CanInteract(user, target))
            return false;

        if (user == target)
            return comp.CanTargetSelf;

        var targetAction = Comp<TargetActionComponent>(uid);
        var coords = Transform(target).Coordinates;
        if (!ValidateBaseTarget(user, coords, (uid, targetAction)))
        {
            // if not just checking pure range, let stored entities be targeted by actions
            // if it's out of range it probably isn't stored anyway...
            return targetAction.CheckCanAccess && _interaction.CanAccessViaStorage(user, target);
        }

        return _interaction.InRangeAndAccessible(user, target, range: targetAction.Range);
    }

    public bool ValidateWorldTarget(EntityUid user, EntityCoordinates target, Entity<WorldTargetActionComponent> ent)
    {
        var targetAction = Comp<TargetActionComponent>(ent);
        return ValidateBaseTarget(user, target, (ent, targetAction));
    }

    private bool ValidateBaseTarget(EntityUid user, EntityCoordinates coords, Entity<TargetActionComponent> ent)
    {
        var (uid, comp) = ent;
        if (comp.CheckCanAccess)
            return _interaction.InRangeUnobstructed(user, coords, range: comp.Range);

        // even if we don't check for obstructions, we may still need to check the range.
        var xform = Transform(user);
        if (xform.MapID != coords.GetMapId(EntityManager))
            return false;

        if (comp.Range <= 0)
            return true;

        return coords.InRange(EntityManager, _transform, xform.Coordinates, comp.Range);
    }

    private void OnInstantSetEvent(Entity<InstantActionComponent> ent, ref ActionSetEventEvent args)
    {
        if (args.Event is InstantActionEvent ev)
        {
            ent.Comp.Event = ev;
            Dirty(ent, ent.Comp);
            args.Handled = true;
        }
    }

    private void OnEntitySetEvent(Entity<EntityTargetActionComponent> ent, ref ActionSetEventEvent args)
    {
        if (args.Event is EntityTargetActionEvent ev)
        {
            ent.Comp.Event = ev;
            Dirty(ent, ent.Comp);
            args.Handled = true;
        }
    }

    private void OnWorldSetEvent(Entity<WorldTargetActionComponent> ent, ref ActionSetEventEvent args)
    {
        if (args.Event is WorldTargetActionEvent ev)
        {
            ent.Comp.Event = ev;
            Dirty(ent, ent.Comp);
            args.Handled = true;
        }
    }

    public void PerformAction(EntityUid performer, ActionsComponent? component, EntityUid actionId, ActionComponent action, BaseActionEvent? actionEvent, TimeSpan curTime, bool predicted = true)
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
            actionEvent.Performer = performer;

            // This here is required because of client-side prediction (RaisePredictiveEvent results in event re-use).
            actionEvent.Handled = false;
            var target = performer;

            if (!action.RaiseOnUser && action.Container is {} container && !_mindQuery.HasComp(container))
                target = container;

            actionEvent.Action = actionId;
            RaiseLocalEvent(target, (object) actionEvent, broadcast: true);
            handled = actionEvent.Handled;
        }

        if (!handled)
            return; // no interaction occurred.

        // play sound, reduce charges, start cooldown, and mark as dirty (if required).

        _audio.PlayPredicted(action.Sound, performer, predicted ? performer : null);

        var dirty = toggledBefore == action.Toggled;

        // TODO: use LimitedCharges
        if (action.Charges != null)
        {
            dirty = true;
            action.Charges--;
            if (action is { Charges: 0, RenewCharges: false })
                action.Enabled = false;
        }

        // TODO: move to ActionCooldown
        action.Cooldown = null;
        if (action is { UseDelay: not null, Charges: null or < 1 })
        {
            dirty = true;
            action.Cooldown = new ActionCooldown
            {
                Start = curTime,
                End = curTime + action.UseDelay.Value
            };
        }

        Dirty(actionId, action);

        if (dirty && component != null)
            Dirty(performer, component); // TODO: it modifies the action component then dirties the USER?!

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
        [NotNullWhen(true)] out ActionComponent? action,
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
        ActionComponent? action = null,
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
        ActionComponent? action = null)
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
    protected virtual void ActionAdded(EntityUid performer, EntityUid actionId, ActionsComponent comp, ActionComponent action)
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

    public IEnumerable<Entity<ActionComponent>> GetActions(EntityUid holderId, ActionsComponent? actions = null)
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

    public void RemoveAction(EntityUid performer, EntityUid? actionId, ActionsComponent? comp = null, ActionComponent? action = null)
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
    protected virtual void ActionRemoved(EntityUid performer, EntityUid actionId, ActionsComponent comp, ActionComponent action)
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

    public void SetEntityIcon(Entity<ActionComponent> ent, EntityUid? icon)
    {
        if (ent.Comp.EntityIcon == icon)
            return;

        ent.Comp.EntityIcon = icon;
        Dirty(ent, ent.Comp);
    }

    public void SetIcon(Entity<ActionComponent> ent, SpriteSpecifier? icon)
    {
        if (ent.Comp.Icon == icon)
            return;

        ent.Comp.Icon = icon;
        Dirty(ent, ent.Comp);
    }

    public void SetIconOn(Entity<ActionComponent> ent, SpriteSpecifier? iconOn)
    {
        if (ent.Comp.IconOn == iconOn)
            return;

        ent.Comp.IconOn = iconOn;
        Dirty(ent, ent.Comp);
    }

    public void SetIconColor(Entity<ActionComponent> ent, Color color)
    {
        if (ent.Comp.IconColor == color)
            return;

        ent.Comp.IconColor = color;
        Dirty(ent, ent.Comp);
    }

    /// <summary>
    /// Set the event of an action.
    /// Since the event isn't required to be serializable this is not networked.
    /// Only use this if it's predicted or for a clientside action.
    /// </summary>
    public void SetEvent(EntityUid uid, BaseActionEvent ev)
    {
        // now this is meta
        var setEv = new ActionSetEventEvent(ev);
        RaiseLocalEvent(uid, ref setEv);
        if (!setEv.Handled)
            Log.Error($"Tried to set event of {ToPrettyString(uid):action} but nothing handled it!");
    }
}
