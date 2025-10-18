using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
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

public abstract partial class SharedActionsSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private   readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private   readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private   readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private   readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private   readonly RotateToFaceSystem _rotateToFace = default!;
    [Dependency] private   readonly SharedAudioSystem _audio = default!;
    [Dependency] private   readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private   readonly SharedTransformSystem _transform = default!;
    [Dependency] private   readonly SharedDoAfterSystem _doAfter = default!;

    private EntityQuery<ActionComponent> _actionQuery;
    private EntityQuery<ActionsComponent> _actionsQuery;
    private EntityQuery<MindComponent> _mindQuery;

    public override void Initialize()
    {
        base.Initialize();
        InitializeActionDoAfter();

        _actionQuery = GetEntityQuery<ActionComponent>();
        _actionsQuery = GetEntityQuery<ActionsComponent>();
        _mindQuery = GetEntityQuery<MindComponent>();

        SubscribeLocalEvent<ActionComponent, MapInitEvent>(OnActionMapInit);

        SubscribeLocalEvent<ActionComponent, ComponentShutdown>(OnActionShutdown);

        SubscribeLocalEvent<ActionsComponent, ActionComponentChangeEvent>(OnActionCompChange);
        SubscribeLocalEvent<ActionsComponent, RelayedActionComponentChangeEvent>(OnRelayActionCompChange);
        SubscribeLocalEvent<ActionsComponent, DidEquipEvent>(OnDidEquip);
        SubscribeLocalEvent<ActionsComponent, DidEquipHandEvent>(OnHandEquipped);
        SubscribeLocalEvent<ActionsComponent, DidUnequipEvent>(OnDidUnequip);
        SubscribeLocalEvent<ActionsComponent, DidUnequipHandEvent>(OnHandUnequipped);
        SubscribeLocalEvent<ActionsComponent, RejuvenateEvent>(OnRejuventate);

        SubscribeLocalEvent<ActionsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ActionsComponent, ComponentGetState>(OnGetState);

        SubscribeLocalEvent<ActionComponent, ActionValidateEvent>(OnValidate);
        SubscribeLocalEvent<InstantActionComponent, ActionValidateEvent>(OnInstantValidate);
        SubscribeLocalEvent<EntityTargetActionComponent, ActionValidateEvent>(OnEntityValidate);
        SubscribeLocalEvent<WorldTargetActionComponent, ActionValidateEvent>(OnWorldValidate);

        SubscribeLocalEvent<InstantActionComponent, ActionGetEventEvent>(OnInstantGetEvent);
        SubscribeLocalEvent<EntityTargetActionComponent, ActionGetEventEvent>(OnEntityGetEvent);
        SubscribeLocalEvent<WorldTargetActionComponent, ActionGetEventEvent>(OnWorldGetEvent);

        SubscribeLocalEvent<InstantActionComponent, ActionSetEventEvent>(OnInstantSetEvent);
        SubscribeLocalEvent<EntityTargetActionComponent, ActionSetEventEvent>(OnEntitySetEvent);
        SubscribeLocalEvent<WorldTargetActionComponent, ActionSetEventEvent>(OnWorldSetEvent);

        SubscribeLocalEvent<EntityTargetActionComponent, ActionSetTargetEvent>(OnEntitySetTarget);
        SubscribeLocalEvent<WorldTargetActionComponent, ActionSetTargetEvent>(OnWorldSetTarget);

        SubscribeAllEvent<RequestPerformActionEvent>(OnActionRequest);
    }

    private void OnActionMapInit(Entity<ActionComponent> ent, ref MapInitEvent args)
    {
        var comp = ent.Comp;
        comp.OriginalIconColor = comp.IconColor;
        DirtyField(ent, ent.Comp, nameof(ActionComponent.OriginalIconColor));
    }

    private void OnActionShutdown(Entity<ActionComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.AttachedEntity is {} user && !TerminatingOrDeleted(user))
            RemoveAction(user, (ent, ent));
    }

    private void OnShutdown(Entity<ActionsComponent> ent, ref ComponentShutdown args)
    {
        foreach (var actionId in ent.Comp.Actions)
        {
            RemoveAction((ent, ent), actionId);
        }
    }

    private void OnGetState(Entity<ActionsComponent> ent, ref ComponentGetState args)
    {
        args.State = new ActionsComponentState(GetNetEntitySet(ent.Comp.Actions));
    }

    /// <summary>
    /// Resolving an action's <see cref="ActionComponent"/>, only returning a value if it exists and has it.
    /// </summary>
    public Entity<ActionComponent>? GetAction(Entity<ActionComponent?>? action, bool logError = true)
    {
        if (action is not {} ent || Deleted(ent))
            return null;

        if (!_actionQuery.Resolve(ent, ref ent.Comp, logError))
            return null;

        return (ent, ent.Comp);
    }

    public void SetCooldown(Entity<ActionComponent?>? action, TimeSpan start, TimeSpan end)
    {
        if (GetAction(action) is not {} ent)
            return;

        ent.Comp.Cooldown = new ActionCooldown
        {
            Start = start,
            End = end
        };
        DirtyField(ent, ent.Comp, nameof(ActionComponent.Cooldown));
    }

    public void RemoveCooldown(Entity<ActionComponent?>? action)
    {
        if (GetAction(action) is not {} ent)
            return;

        ent.Comp.Cooldown = null;
        DirtyField(ent, ent.Comp, nameof(ActionComponent.Cooldown));
    }

    /// <summary>
    /// Starts a cooldown starting now, lasting for <c>cooldown</c> seconds.
    /// </summary>
    public void SetCooldown(Entity<ActionComponent?>? action, TimeSpan cooldown)
    {
        var start = GameTiming.CurTime;
        SetCooldown(action, start, start + cooldown);
    }

    public void ClearCooldown(Entity<ActionComponent?>? action)
    {
        if (GetAction(action) is not {} ent)
            return;

        if (ent.Comp.Cooldown is not {} cooldown)
            return;

        ent.Comp.Cooldown = new ActionCooldown
        {
            Start = cooldown.Start,
            End = GameTiming.CurTime
        };
        DirtyField(ent, ent.Comp, nameof(ActionComponent.Cooldown));
    }

    /// <summary>
    ///     Sets the cooldown for this action only if it is bigger than the one it already has.
    /// </summary>
    public void SetIfBiggerCooldown(Entity<ActionComponent?>? action, TimeSpan cooldown)
    {
        if (GetAction(action) is not {} ent || cooldown < TimeSpan.Zero)
            return;

        var start = GameTiming.CurTime;
        var end = start + cooldown;
        if (ent.Comp.Cooldown?.End > end)
            return;

        SetCooldown((ent, ent), start, end);
    }

    /// <summary>
    /// Set an action's cooldown to its use delay, if it has one.
    /// If there is no set use delay this does nothing.
    /// </summary>
    public void StartUseDelay(Entity<ActionComponent?>? action)
    {
        if (GetAction(action) is not {} ent || ent.Comp.UseDelay is not {} delay)
            return;

        SetCooldown((ent, ent), delay);
    }

    public void SetUseDelay(Entity<ActionComponent?>? action, TimeSpan? delay)
    {
        if (GetAction(action) is not {} ent || ent.Comp.UseDelay == delay)
            return;

        ent.Comp.UseDelay = delay;
        UpdateAction(ent);
        DirtyField(ent, ent.Comp, nameof(ActionComponent.UseDelay));
    }

    public void ReduceUseDelay(Entity<ActionComponent?>? action, TimeSpan? lowerDelay)
    {
        if (GetAction(action) is not {} ent)
            return;

        if (ent.Comp.UseDelay != null && lowerDelay != null)
            ent.Comp.UseDelay -= lowerDelay;

        if (ent.Comp.UseDelay < TimeSpan.Zero)
            ent.Comp.UseDelay = null;

        UpdateAction(ent);
        DirtyField(ent, ent.Comp, nameof(ActionComponent.UseDelay));
    }

    private void OnRejuventate(Entity<ActionsComponent> ent, ref RejuvenateEvent args)
    {
        foreach (var act in ent.Comp.Actions)
        {
            ClearCooldown(act);
        }
    }

    #region ComponentStateManagement
    public virtual void UpdateAction(Entity<ActionComponent> ent)
    {
        // See client-side code.
    }

    public void SetToggled(Entity<ActionComponent?>? action, bool toggled)
    {
        if (GetAction(action) is not {} ent || ent.Comp.Toggled == toggled)
            return;

        ent.Comp.Toggled = toggled;
        UpdateAction(ent);
        DirtyField(ent, ent.Comp, nameof(ActionComponent.Toggled));
    }

    public void SetEnabled(Entity<ActionComponent?>? action, bool enabled)
    {
        if (GetAction(action) is not {} ent || ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        UpdateAction(ent);
        DirtyField(ent, ent.Comp, nameof(ActionComponent.Enabled));
    }

    #endregion

    #region Execution
    /// <summary>
    ///     When receiving a request to perform an action, this validates whether the action is allowed. If it is, it
    ///     will raise the relevant action event
    /// </summary>
    private void OnActionRequest(RequestPerformActionEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        TryPerformAction(ev, user);
    }

    /// <summary>
    /// <see cref="OnActionRequest"/>
    /// </summary>
    /// <param name="ev">The Request Perform Action Event</param>
    /// <param name="user">The user/performer of the action</param>
    /// <param name="skipDoActionRequest">Should this skip the initial doaction request?</param>
    private bool TryPerformAction(RequestPerformActionEvent ev, EntityUid user, bool skipDoActionRequest = false)
    {
        if (!_actionsQuery.TryComp(user, out var component))
            return false;

        var actionEnt = GetEntity(ev.Action);

        if (!TryComp(actionEnt, out MetaDataComponent? metaData))
            return false;

        var name = Name(actionEnt, metaData);

        // Does the user actually have the requested action?
        if (!component.Actions.Contains(actionEnt))
        {
            _adminLogger.Add(LogType.Action,
                $"{ToPrettyString(user):user} attempted to perform an action that they do not have: {name}.");
            return false;
        }

        if (GetAction(actionEnt) is not {} action)
            return false;

        DebugTools.Assert(action.Comp.AttachedEntity == user);
        if (!action.Comp.Enabled)
            return false;

        var curTime = GameTiming.CurTime;
        if (IsCooldownActive(action, curTime))
            return false;

        // check for action use prevention
        var attemptEv = new ActionAttemptEvent(user);
        RaiseLocalEvent(action, ref attemptEv);
        if (attemptEv.Cancelled)
            return false;

        // Validate request by checking action blockers and the like
        var provider = action.Comp.Container ?? user;
        var validateEv = new ActionValidateEvent()
        {
            Input = ev,
            User = user,
            Provider = provider
        };
        RaiseLocalEvent(action, ref validateEv);
        if (validateEv.Invalid)
            return false;

        if (TryComp<DoAfterArgsComponent>(action, out var actionDoAfterComp) && TryComp<DoAfterComponent>(user, out var performerDoAfterComp) && !skipDoActionRequest)
        {
            return TryStartActionDoAfter((action, actionDoAfterComp), (user, performerDoAfterComp), action.Comp.UseDelay, ev);
        }

        // All checks passed. Perform the action!
        PerformAction((user, component), action);
        return true;
    }

    private void OnValidate(Entity<ActionComponent> ent, ref ActionValidateEvent args)
    {
        if ((ent.Comp.CheckConsciousness && !_actionBlocker.CanConsciouslyPerformAction(args.User))
            || (ent.Comp.CheckCanInteract && !_actionBlocker.CanInteract(args.User, null)))
            args.Invalid = true;
    }

    private void OnInstantValidate(Entity<InstantActionComponent> ent, ref ActionValidateEvent args)
    {
        _adminLogger.Add(LogType.Action,
            $"{ToPrettyString(args.User):user} is performing the {Name(ent):action} action provided by {ToPrettyString(args.Provider):provider}.");
    }

    private void OnEntityValidate(Entity<EntityTargetActionComponent> ent, ref ActionValidateEvent args)
    {
        // let WorldTargetAction handle it
        if (ent.Comp.Event is not {} ev)
        {
            DebugTools.Assert(HasComp<WorldTargetActionComponent>(ent), $"Entity-world targeting action {ToPrettyString(ent)} requires WorldTargetActionComponent");
            return;
        }

        if (args.Input.EntityTarget is not {} netTarget)
        {
            args.Invalid = true;
            return;
        }

        var user = args.User;

        var target = GetEntity(netTarget);

        var targetWorldPos = _transform.GetWorldPosition(target);

        if (ent.Comp.RotateOnUse)
            _rotateToFace.TryFaceCoordinates(user, targetWorldPos);

        if (!ValidateEntityTarget(user, target, ent))
            return;

        _adminLogger.Add(LogType.Action,
            $"{ToPrettyString(user):user} is performing the {Name(ent):action} action (provided by {ToPrettyString(args.Provider):provider}) targeted at {ToPrettyString(target):target}.");

        ev.Target = target;
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

        if (ent.Comp.RotateOnUse)
            _rotateToFace.TryFaceCoordinates(user, _transform.ToMapCoordinates(target).Position);

        if (!ValidateWorldTarget(user, target, ent))
            return;

        // if the client specified an entity it needs to be valid
        var targetEntity = GetEntity(args.Input.EntityTarget);
        if (targetEntity != null && (
            !TryComp<EntityTargetActionComponent>(ent, out var entTarget) ||
            !ValidateEntityTarget(user, targetEntity.Value, (ent, entTarget))))
        {
            args.Invalid = true;
            return;
        }

        _adminLogger.Add(LogType.Action,
            $"{ToPrettyString(user):user} is performing the {Name(ent):action} action (provided by {args.Provider}) targeting {targetEntity} at {target:target}.");

        if (ent.Comp.Event is {} ev)
        {
            ev.Target = target;
            ev.Entity = targetEntity;
        }
    }

    public bool ValidateEntityTarget(EntityUid user, EntityUid target, Entity<EntityTargetActionComponent> ent)
    {
        var (uid, comp) = ent;
        if (!target.IsValid() || Deleted(target))
            return false;

        if (_whitelist.IsWhitelistFail(comp.Whitelist, target))
            return false;

        if (_whitelist.IsWhitelistPass(comp.Blacklist, target))
            return false;

        if (_actionQuery.Comp(uid).CheckCanInteract && !_actionBlocker.CanInteract(user, target))
            return false;

        if (user == target)
            return comp.CanTargetSelf;

        var targetAction = Comp<TargetActionComponent>(uid);

        // not using the ValidateBaseTarget logic since its raycast fails if the target is e.g. a wall
        if (targetAction.CheckCanAccess)
            return _interaction.InRangeAndAccessible(user, target, targetAction.Range, targetAction.AccessMask);

        // Just check normal in range, allowing <= 0 range to mean infinite range.
        if (targetAction.Range > 0
            && !_transform.InRange(user, target, targetAction.Range))
            return false;

        // If checkCanAccess isn't set, we allow targeting things in containers
        return _interaction.IsAccessible(user, target);
    }

    public bool ValidateWorldTarget(EntityUid user, EntityCoordinates target, Entity<WorldTargetActionComponent> ent)
    {
        var targetAction = Comp<TargetActionComponent>(ent);
        return ValidateBaseTarget(user, target, (ent, targetAction));
    }

    private bool ValidateBaseTarget(EntityUid user, EntityCoordinates coords, Entity<TargetActionComponent> ent)
    {
        var comp = ent.Comp;
        if (comp.CheckCanAccess)
            return _interaction.InRangeUnobstructed(user, coords, range: comp.Range);

        // even if we don't check for obstructions, we may still need to check the range.
        var xform = Transform(user);
        if (xform.MapID != _transform.GetMapId(coords))
            return false;

        if (comp.Range <= 0)
            return true;

        return _transform.InRange(coords, xform.Coordinates, comp.Range);
    }

    private void OnInstantGetEvent(Entity<InstantActionComponent> ent, ref ActionGetEventEvent args)
    {
        if (ent.Comp.Event is {} ev)
            args.Event = ev;
    }

    private void OnEntityGetEvent(Entity<EntityTargetActionComponent> ent, ref ActionGetEventEvent args)
    {
        if (ent.Comp.Event is {} ev)
            args.Event = ev;
    }

    private void OnWorldGetEvent(Entity<WorldTargetActionComponent> ent, ref ActionGetEventEvent args)
    {
        if (ent.Comp.Event is {} ev)
            args.Event = ev;
    }

    private void OnInstantSetEvent(Entity<InstantActionComponent> ent, ref ActionSetEventEvent args)
    {
        if (args.Event is InstantActionEvent ev)
        {
            ent.Comp.Event = ev;
            args.Handled = true;
        }
    }

    private void OnEntitySetEvent(Entity<EntityTargetActionComponent> ent, ref ActionSetEventEvent args)
    {
        if (args.Event is EntityTargetActionEvent ev)
        {
            ent.Comp.Event = ev;
            args.Handled = true;
        }
    }

    private void OnWorldSetEvent(Entity<WorldTargetActionComponent> ent, ref ActionSetEventEvent args)
    {
        if (args.Event is WorldTargetActionEvent ev)
        {
            ent.Comp.Event = ev;
            args.Handled = true;
        }
    }

    private void OnEntitySetTarget(Entity<EntityTargetActionComponent> ent, ref ActionSetTargetEvent args)
    {
        if (ent.Comp.Event is {} ev)
        {
            ev.Target = args.Target;
            args.Handled = true;
        }
    }

    private void OnWorldSetTarget(Entity<WorldTargetActionComponent> ent, ref ActionSetTargetEvent args)
    {
        if (ent.Comp.Event is {} ev)
        {
            ev.Target = Transform(args.Target).Coordinates;
            // only set Entity if the action also has EntityTargetAction
            ev.Entity = HasComp<EntityTargetActionComponent>(ent) ? args.Target : null;
            args.Handled = true;
        }
    }

    /// <summary>
    /// Perform an action, bypassing validation checks.
    /// </summary>
    /// <param name="performer">The entity performing the action</param>
    /// <param name="action">The action being performed</param>
    /// <param name="actionEvent">An event override to perform. If null, uses <see cref="GetEvent"/></param>
    /// <param name="predicted">If false, prevents playing the action's sound on the client</param>
    public void PerformAction(Entity<ActionsComponent?> performer, Entity<ActionComponent> action, BaseActionEvent? actionEvent = null, bool predicted = true)
    {
        var handled = false;

        // Note that attached entity and attached container are allowed to be null here.
        if (action.Comp.AttachedEntity != null && action.Comp.AttachedEntity != performer)
        {
            Log.Error($"{ToPrettyString(performer)} is attempting to perform an action {ToPrettyString(action)} that is attached to another entity {ToPrettyString(action.Comp.AttachedEntity)}");
            return;
        }

        actionEvent ??= GetEvent(action);

        if (actionEvent is not {} ev)
            return;

        ev.Performer = performer;

        // This here is required because of client-side prediction (RaisePredictiveEvent results in event re-use).
        ev.Handled = false;
        var target = performer.Owner;
        ev.Performer = performer;
        ev.Action = action;

        // TODO: This is where we'd add support for event lists
        if (!action.Comp.RaiseOnUser && action.Comp.Container is {} container && !_mindQuery.HasComp(container))
            target = container;

        if (action.Comp.RaiseOnAction)
            target = action;

        RaiseLocalEvent(target, (object) ev, broadcast: true);
        handled = ev.Handled;

        if (!handled)
            return; // no interaction occurred.

        // play sound, start cooldown
        if (ev.Toggle)
            SetToggled((action, action), !action.Comp.Toggled);

        _audio.PlayPredicted(action.Comp.Sound, performer, predicted ? performer : null);

        RemoveCooldown((action, action));
        StartUseDelay((action, action));

        UpdateAction(action);

        var performed = new ActionPerformedEvent(performer);
        RaiseLocalEvent(action, ref performed);
    }
    #endregion

    #region AddRemoveActions

    public EntityUid? AddAction(EntityUid performer,
        [ForbidLiteral] string? actionPrototypeId,
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
        [ForbidLiteral] string? actionPrototypeId,
        EntityUid container = default,
        ActionsComponent? component = null)
    {
        return AddAction(performer, ref actionId, out _, actionPrototypeId, container, component);
    }

    /// <inheritdoc cref="AddAction(Robust.Shared.GameObjects.EntityUid,ref System.Nullable{Robust.Shared.GameObjects.EntityUid},string?,Robust.Shared.GameObjects.EntityUid,ActionsComponent?)"/>
    public bool AddAction(EntityUid performer,
        [NotNullWhen(true)] ref EntityUid? actionId,
        [NotNullWhen(true)] out ActionComponent? action,
        [ForbidLiteral] string? actionPrototypeId,
        EntityUid container = default,
        ActionsComponent? component = null)
    {
        if (!container.IsValid())
            container = performer;

        if (!_actionContainer.EnsureAction(container, ref actionId, out action, actionPrototypeId))
            return false;

        return AddActionDirect((performer, component), (actionId.Value, action));
    }

    /// <summary>
    ///     Adds a pre-existing action.
    /// </summary>
    public bool AddAction(Entity<ActionsComponent?> performer,
        Entity<ActionComponent?> action,
        Entity<ActionsContainerComponent?> container)
    {
        if (GetAction(action) is not {} ent)
            return false;

        if (ent.Comp.Container != container.Owner
            || !Resolve(container, ref container.Comp)
            || !container.Comp.Container.Contains(ent))
        {
            Log.Error($"Attempted to add an action with an invalid container: {ToPrettyString(ent)}");
            return false;
        }

        return AddActionDirect(performer, (ent, ent));
    }

    /// <summary>
    ///     Adds a pre-existing action. This also bypasses the requirement that the given action must be stored in a
    ///     valid action container.
    /// </summary>
    public bool AddActionDirect(Entity<ActionsComponent?> performer,
        Entity<ActionComponent?>? action)
    {
        if (GetAction(action) is not {} ent)
            return false;

        DebugTools.Assert(ent.Comp.Container == null ||
                          (TryComp(ent.Comp.Container, out ActionsContainerComponent? containerComp)
                           && containerComp.Container.Contains(ent)));

        if (ent.Comp.AttachedEntity is {} user)
            RemoveAction(user, (ent, ent));

        // TODO: make this an event bruh
        if (ent.Comp.StartDelay && ent.Comp.UseDelay != null)
            SetCooldown((ent, ent), ent.Comp.UseDelay.Value);

        DebugTools.AssertOwner(performer, performer.Comp);
        performer.Comp ??= EnsureComp<ActionsComponent>(performer);
        ent.Comp.AttachedEntity = performer;
        DirtyField(ent, ent.Comp, nameof(ActionComponent.AttachedEntity));
        performer.Comp.Actions.Add(ent);
        Dirty(performer, performer.Comp);
        ActionAdded((performer, performer.Comp), (ent, ent.Comp));
        return true;
    }

    /// <summary>
    /// This method gets called after a new action got added.
    /// </summary>
    protected virtual void ActionAdded(Entity<ActionsComponent> performer, Entity<ActionComponent> action)
    {
        // See client-side system for UI code.
    }

    /// <summary>
    ///     Grant pre-existing actions. If the entity has no action component, this will give them one.
    /// </summary>
    /// <param name="performer">Entity to receive the actions</param>
    /// <param name="actions">The actions to add</param>
    /// <param name="container">The entity that enables these actions (e.g., flashlight). May be null (innate actions).</param>
    public void GrantActions(Entity<ActionsComponent?> performer,
        IEnumerable<EntityUid> actions,
        Entity<ActionsContainerComponent?> container)
    {
        if (!Resolve(container, ref container.Comp))
            return;

        DebugTools.AssertOwner(performer, performer.Comp);
        performer.Comp ??= EnsureComp<ActionsComponent>(performer);

        foreach (var actionId in actions)
        {
            AddAction(performer, actionId, container);
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
            if (GetAction(actionId) is {} action)
                AddActionDirect(performer, (action, action));
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

        AddActionDirect(performer, actionId);
    }

    public IEnumerable<Entity<ActionComponent>> GetActions(EntityUid holderId, ActionsComponent? actions = null)
    {
        if (!Resolve(holderId, ref actions, false))
            yield break;

        foreach (var actionId in actions.Actions)
        {
            if (GetAction(actionId) is not {} ent)
                continue;

            yield return ent;
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
            if (GetAction(actionId) is not {} ent)
                return;

            if (ent.Comp.Container == container)
                RemoveAction((performer, comp), (ent, ent));
        }
    }

    /// <summary>
    ///     Removes a single provided action provided by another entity.
    /// </summary>
    public void RemoveProvidedAction(EntityUid performer, EntityUid container, EntityUid actionId, ActionsComponent? comp = null)
    {
        if (!_actionsQuery.Resolve(performer, ref comp, false) || GetAction(actionId) is not {} ent)
            return;

        if (ent.Comp.Container == container)
            RemoveAction((performer, comp), (ent, ent));
    }

    /// <summary>
    /// Removes an action from its container, if it still exists.
    /// </summary>
    public void RemoveAction(Entity<ActionComponent?>? action)
    {
        if (GetAction(action) is not {} ent || ent.Comp.AttachedEntity is not {} actions)
            return;

        if (!_actionsQuery.TryComp(actions, out var comp))
            return;

        RemoveAction((actions, comp), (ent, ent));
    }

    public void RemoveAction(Entity<ActionsComponent?> performer, Entity<ActionComponent?>? action)
    {
        if (GetAction(action) is not {} ent)
            return;

        if (ent.Comp.AttachedEntity != performer.Owner)
        {
            DebugTools.Assert(!Resolve(performer, ref performer.Comp, false)
                              || performer.Comp.LifeStage >= ComponentLifeStage.Stopping
                              || !performer.Comp.Actions.Contains(ent.Owner));

            if (!GameTiming.ApplyingState)
                Log.Error($"Attempted to remove an action {ToPrettyString(ent)} from an entity that it was never attached to: {ToPrettyString(performer)}. Trace: {Environment.StackTrace}");
            return;
        }

        if (!_actionsQuery.Resolve(performer, ref performer.Comp, false))
        {
            DebugTools.Assert(TerminatingOrDeleted(performer));
            ent.Comp.AttachedEntity = null;
            // TODO: should this delete the action since it's now orphaned?
            return;
        }

        performer.Comp.Actions.Remove(ent.Owner);
        Dirty(performer, performer.Comp);
        ent.Comp.AttachedEntity = null;
        DirtyField(ent, ent.Comp, nameof(ActionComponent.AttachedEntity));
        ActionRemoved((performer, performer.Comp), ent);

        if (ent.Comp.Temporary)
            QueueDel(ent);
    }

    /// <summary>
    /// This method gets called after an action got removed.
    /// </summary>
    protected virtual void ActionRemoved(Entity<ActionsComponent> performer, Entity<ActionComponent> action)
    {
        // See client-side system for UI code.
    }

    public bool ValidAction(Entity<ActionComponent> ent, bool canReach = true)
    {
        var (uid, comp) = ent;
        if (!comp.Enabled)
            return false;

        var curTime = GameTiming.CurTime;
        if (comp.Cooldown.HasValue && comp.Cooldown.Value.End > curTime)
            return false;

        // TODO: use event for this
        return canReach || Comp<TargetActionComponent>(ent)?.CheckCanAccess == false;
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
    private void OnDidEquip(Entity<ActionsComponent> ent, ref DidEquipEvent args)
    {
        if (GameTiming.ApplyingState)
            return;

        var ev = new GetItemActionsEvent(_actionContainer, args.Equipee, args.Equipment, args.SlotFlags);
        RaiseLocalEvent(args.Equipment, ev);

        if (ev.Actions.Count == 0)
            return;

        GrantActions((ent, ent), ev.Actions, args.Equipment);
    }

    private void OnHandEquipped(Entity<ActionsComponent> ent, ref DidEquipHandEvent args)
    {
        if (GameTiming.ApplyingState)
            return;

        var ev = new GetItemActionsEvent(_actionContainer, args.User, args.Equipped);
        RaiseLocalEvent(args.Equipped, ev);

        if (ev.Actions.Count == 0)
            return;

        GrantActions((ent, ent), ev.Actions, args.Equipped);
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

    public void SetEntityIcon(Entity<ActionComponent?> ent, EntityUid? icon)
    {
        if (!_actionQuery.Resolve(ent, ref ent.Comp) || ent.Comp.EntityIcon == icon)
            return;

        ent.Comp.EntityIcon = icon;
        DirtyField(ent, ent.Comp, nameof(ActionComponent.EntIcon));
    }

    public void SetIcon(Entity<ActionComponent?> ent, SpriteSpecifier? icon)
    {
        if (!_actionQuery.Resolve(ent, ref ent.Comp) || ent.Comp.Icon == icon)
            return;

        ent.Comp.Icon = icon;
        DirtyField(ent, ent.Comp, nameof(ActionComponent.Icon));
    }

    public void SetIconOn(Entity<ActionComponent?> ent, SpriteSpecifier? iconOn)
    {
        if (!_actionQuery.Resolve(ent, ref ent.Comp) || ent.Comp.IconOn == iconOn)
            return;

        ent.Comp.IconOn = iconOn;
        DirtyField(ent, ent.Comp, nameof(ActionComponent.IconOn));
    }

    public void SetIconColor(Entity<ActionComponent?> ent, Color color)
    {
        if (!_actionQuery.Resolve(ent, ref ent.Comp) || ent.Comp.IconColor == color)
            return;

        ent.Comp.IconColor = color;
        DirtyField(ent, ent.Comp, nameof(ActionComponent.IconColor));
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

    public BaseActionEvent? GetEvent(EntityUid uid)
    {
        DebugTools.Assert(_actionQuery.HasComp(uid), $"Entity {ToPrettyString(uid)} is missing ActionComponent");
        var ev = new ActionGetEventEvent();
        RaiseLocalEvent(uid, ref ev);
        return ev.Event;
    }

    public bool SetEventTarget(EntityUid uid, EntityUid target)
    {
        DebugTools.Assert(_actionQuery.HasComp(uid), $"Entity {ToPrettyString(uid)} is missing ActionComponent");
        var ev = new ActionSetTargetEvent(target);
        RaiseLocalEvent(uid, ref ev);
        return ev.Handled;
    }

    /// <summary>
    ///     Checks if the action has a cooldown and if it's still active
    /// </summary>
    public bool IsCooldownActive(ActionComponent action, TimeSpan? curTime = null)
    {
        // TODO: Check for charge recovery timer
        curTime ??= GameTiming.CurTime;
        return action.Cooldown.HasValue && action.Cooldown.Value.End > curTime;
    }

    /// <summary>
    /// Marks the action as temporary.
    /// Temporary actions get deleted upon being removed from an entity.
    /// </summary>
    public void SetTemporary(Entity<ActionComponent?> ent, bool temporary)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.Temporary = temporary;
        Dirty(ent);
    }
}
