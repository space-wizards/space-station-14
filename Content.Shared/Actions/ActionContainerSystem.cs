using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Actions.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Actions;

/// <summary>
/// Handles storing & spawning action entities in a container.
/// </summary>
public sealed class ActionContainerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private EntityQuery<ActionComponent> _query;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<ActionComponent>();

        SubscribeLocalEvent<ActionsContainerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ActionsContainerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ActionsContainerComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);
        SubscribeLocalEvent<ActionsContainerComponent, EntInsertedIntoContainerMessage>(OnEntityInserted);
        SubscribeLocalEvent<ActionsContainerComponent, ActionAddedEvent>(OnActionAdded);
        SubscribeLocalEvent<ActionsContainerComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<ActionsContainerComponent, MindRemovedMessage>(OnMindRemoved);
    }

    private void OnMindAdded(EntityUid uid, ActionsContainerComponent component, MindAddedMessage args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out _))
            return;
        if (!TryComp<ActionsContainerComponent>(mindId, out var mindActionContainerComp))
            return;

        if (!HasComp<GhostComponent>(uid) && mindActionContainerComp.Container.ContainedEntities.Count > 0 )
            _actions.GrantContainedActions(uid, mindId);
    }

    private void OnMindRemoved(EntityUid uid, ActionsContainerComponent component, MindRemovedMessage args)
    {
        _actions.RemoveProvidedActions(uid, args.Mind);
    }

    /// <summary>
    /// Spawns a new action entity and adds it to the given container.
    /// </summary>
    public EntityUid? AddAction(EntityUid uid, string actionPrototypeId, ActionsContainerComponent? comp = null)
    {
        EntityUid? result = default;
        EnsureAction(uid, ref result, actionPrototypeId, comp);
        return result;
    }

    /// <summary>
    /// Ensures that a given entityUid refers to a valid entity action contained by the given container.
    /// If the entity does not exist, it will attempt to spawn a new action.
    /// Returns false if the given entity exists, but is not in a valid state.
    /// </summary>
    public bool EnsureAction(EntityUid uid,
        [NotNullWhen(true)] ref EntityUid? actionId,
        string actionPrototypeId,
        ActionsContainerComponent? comp = null)
    {
        return EnsureAction(uid, ref actionId, out _, actionPrototypeId, comp);
    }

    /// <inheritdoc cref="EnsureAction(Robust.Shared.GameObjects.EntityUid,ref System.Nullable{Robust.Shared.GameObjects.EntityUid},string?,Content.Shared.Actions.ActionsContainerComponent?)"/>
    public bool EnsureAction(EntityUid uid,
        [NotNullWhen(true)] ref EntityUid? actionId,
        [NotNullWhen(true)] out ActionComponent? action,
        string? actionPrototypeId,
        ActionsContainerComponent? comp = null)
    {
        action = null;

        DebugTools.AssertOwner(uid, comp);
        comp ??= EnsureComp<ActionsContainerComponent>(uid);

        if (Exists(actionId))
        {
            if (!comp.Container.Contains(actionId.Value))
            {
                Log.Error($"Action {ToPrettyString(actionId.Value)} is not contained in the expected container {ToPrettyString(uid)}");
                return false;
            }

            if (_actions.GetAction(actionId) is not {} ent)
                return false;

            actionId = ent;
            action = ent.Comp;
            DebugTools.Assert(Transform(ent).ParentUid == uid);
            DebugTools.Assert(_container.IsEntityInContainer(ent));
            DebugTools.Assert(ent.Comp.Container == uid);
            return true;
        }

        // Null prototypes are never valid entities, they mean that someone didn't provide a proper prototype.
        if (actionPrototypeId == null)
            return false;

        // Client cannot predict entity spawning.
        if (_netMan.IsClient && !IsClientSide(uid))
            return false;

        actionId = Spawn(actionPrototypeId);
        if (!_query.TryComp(actionId, out action))
        {
            Log.Error($"Tried to add invalid action {ToPrettyString(actionId)} to {ToPrettyString(uid)}!");
            Del(actionId);
            return false;
        }

        if (AddAction(uid, actionId.Value, action, comp))
            return true;

        Del(actionId.Value);
        actionId = null;
        return false;
    }

    /// <summary>
    /// Transfers an action from one container to another, while keeping the attached entity the same.
    /// </summary>
    /// <remarks>
    /// While the attached entity should be the same at the end, this will actually remove and then re-grant the action.
    /// </remarks>
    public void TransferAction(
        EntityUid actionId,
        EntityUid newContainer,
        ActionComponent? action = null,
        ActionsContainerComponent? container = null)
    {
        if (_actions.GetAction((actionId, action)) is not {} ent)
            return;

        if (ent.Comp.Container == newContainer)
            return;

        var attached = ent.Comp.AttachedEntity;
        if (!AddAction(newContainer, ent, ent.Comp, container))
            return;

        DebugTools.AssertEqual(ent.Comp.Container, newContainer);
        DebugTools.AssertEqual(ent.Comp.AttachedEntity, attached);
    }

    /// <summary>
    /// Transfers all actions from one container to another, while keeping the attached entity the same.
    /// </summary>
    /// <remarks>
    /// While the attached entity should be the same at the end, this will actually remove and then re-grant the action.
    /// </remarks>
    public void TransferAllActions(
        EntityUid from,
        EntityUid to,
        ActionsContainerComponent? oldContainer = null,
        ActionsContainerComponent? newContainer = null)
    {
        if (!Resolve(from, ref oldContainer) || !Resolve(to, ref newContainer))
            return;

        foreach (var action in oldContainer.Container.ContainedEntities.ToArray())
        {
            TransferAction(action, to, container: newContainer);
        }

        DebugTools.AssertEqual(oldContainer.Container.Count, 0);
    }

    /// <summary>
    /// Transfers an actions from one container to another, while changing the attached entity.
    /// </summary>
    /// <remarks>
    /// This will actually remove and then re-grant the action.
    /// Useful where you need to transfer from one container to another but also change the attached entity (ie spellbook > mind > user)
    /// </remarks>
    public void TransferActionWithNewAttached(
        EntityUid actionId,
        EntityUid newContainer,
        EntityUid newAttached,
        ActionComponent? action = null,
        ActionsContainerComponent? container = null)
    {
        if (_actions.GetAction((actionId, action)) is not {} ent)
            return;

        if (ent.Comp.Container == newContainer)
            return;

        var attached = newAttached;
        if (!AddAction(newContainer, ent, ent.Comp, container))
            return;

        DebugTools.AssertEqual(ent.Comp.Container, newContainer);
        _actions.AddActionDirect(newAttached, (ent, ent.Comp));

        DebugTools.AssertEqual(ent.Comp.AttachedEntity, attached);
    }

    /// <summary>
    /// Transfers all actions from one container to another, while changing the attached entity.
    /// </summary>
    /// <remarks>
    /// This will actually remove and then re-grant the action.
    /// Useful where you need to transfer from one container to another but also change the attached entity (ie spellbook > mind > user)
    /// </remarks>
    public void TransferAllActionsWithNewAttached(
        EntityUid from,
        EntityUid to,
        EntityUid newAttached,
        ActionsContainerComponent? oldContainer = null,
        ActionsContainerComponent? newContainer = null)
    {
        if (!Resolve(from, ref oldContainer) || !Resolve(to, ref newContainer))
            return;

        foreach (var action in oldContainer.Container.ContainedEntities.ToArray())
        {
            TransferActionWithNewAttached(action, to, newAttached, container: newContainer);
        }

        DebugTools.AssertEqual(oldContainer.Container.Count, 0);
    }

    /// <summary>
    /// Adds a pre-existing action to an action container. If the action is already in some container it will first remove it.
    /// </summary>
    public bool AddAction(EntityUid uid, EntityUid actionId, ActionComponent? action = null, ActionsContainerComponent? comp = null)
    {
        if (_actions.GetAction((actionId, action)) is not {} ent)
            return false;

        if (ent.Comp.Container != null)
            RemoveAction((ent, ent));

        DebugTools.AssertOwner(uid, comp);
        comp ??= EnsureComp<ActionsContainerComponent>(uid);
        if (!_container.Insert(ent.Owner, comp.Container))
        {
            Log.Error($"Failed to insert action {ToPrettyString(ent)} into {ToPrettyString(uid)}");
            return false;
        }

        // Container insert events should have updated the component's fields:
        DebugTools.Assert(comp.Container.Contains(ent));
        DebugTools.Assert(ent.Comp.Container == uid);

        return true;
    }

    /// <summary>
    /// Removes an action from its container and any action-performer and moves the action to null-space
    /// </summary>
    public void RemoveAction(Entity<ActionComponent?>? action, bool logMissing = true)
    {
        if (_actions.GetAction(action, logMissing) is not {} ent)
            return;

        if (ent.Comp.Container == null)
            return;

        _transform.DetachEntity(ent, Transform(ent));

        // Container removal events should have removed the action from the action container.
        // However, just in case the container was already deleted we will still manually clear the container field
        if (ent.Comp.Container is {} container)
        {
            if (Exists(container))
                Log.Error($"Failed to remove action {ToPrettyString(ent)} from its container {ToPrettyString(container)}?");
            ent.Comp.Container = null;
            DirtyField(ent, ent.Comp, nameof(ActionComponent.Container));
        }

        // If the action was granted to some entity, then the removal from the container should have automatically removed it.
        // However, if the action was granted without ever being placed in an action container, it will not have been removed.
        // Therefore, to ensure that the behaviour of the method is consistent we will also explicitly remove the action.
        if (ent.Comp.AttachedEntity is {} actions)
            _actions.RemoveAction(actions, (ent, ent));
    }

    private void OnInit(EntityUid uid, ActionsContainerComponent component, ComponentInit args)
    {
        component.Container = _container.EnsureContainer<Container>(uid, ActionsContainerComponent.ContainerId);
    }

    private void OnShutdown(EntityUid uid, ActionsContainerComponent component, ComponentShutdown args)
    {
        if (_timing.ApplyingState && component.NetSyncEnabled)
            return; // The game state should handle the container removal & action deletion.

        _container.ShutdownContainer(component.Container);
    }

    private void OnEntityInserted(EntityUid uid, ActionsContainerComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ActionsContainerComponent.ContainerId)
            return;

        if (_actions.GetAction(args.Entity) is not {} action)
            return;

        if (action.Comp.Container != uid)
        {
            action.Comp.Container = uid;
            DirtyField(action, action.Comp, nameof(ActionComponent.Container));
        }

        var ev = new ActionAddedEvent(args.Entity, action);
        RaiseLocalEvent(uid, ref ev);
    }

    private void OnEntityRemoved(EntityUid uid, ActionsContainerComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ActionsContainerComponent.ContainerId)
            return;

        if (_actions.GetAction(args.Entity, false) is not {} action)
            return;

        var ev = new ActionRemovedEvent(args.Entity, action);
        RaiseLocalEvent(uid, ref ev);

        if (action.Comp.Container == null)
            return;

        action.Comp.Container = null;
        DirtyField(action, action.Comp, nameof(ActionComponent.Container));
    }

    private void OnActionAdded(EntityUid uid, ActionsContainerComponent component, ActionAddedEvent args)
    {
        if (TryComp<MindComponent>(uid, out var mindComp) && mindComp.OwnedEntity != null && HasComp<ActionsContainerComponent>(mindComp.OwnedEntity.Value))
            _actions.GrantContainedAction(mindComp.OwnedEntity.Value, uid, args.Action);
    }
}

/// <summary>
/// Raised directed at an action container when a new action entity gets inserted.
/// </summary>
[ByRefEvent]
public readonly struct ActionAddedEvent
{
    public readonly EntityUid Action;
    public readonly ActionComponent Component;

    public ActionAddedEvent(EntityUid action, ActionComponent component)
    {
        Action = action;
        Component = component;
    }
}

/// <summary>
/// Raised directed at an action container when an action entity gets removed.
/// </summary>
[ByRefEvent]
public readonly struct ActionRemovedEvent
{
    public readonly EntityUid Action;
    public readonly ActionComponent Component;

    public ActionRemovedEvent(EntityUid action, ActionComponent component)
    {
        Action = action;
        Component = component;
    }
}
