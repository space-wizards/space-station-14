using System.Diagnostics.CodeAnalysis;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionsContainerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ActionsContainerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ActionsContainerComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);
        SubscribeLocalEvent<ActionsContainerComponent, EntInsertedIntoContainerMessage>(OnEntityInserted);
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
        [NotNullWhen(true)] out BaseActionComponent? action,
        string? actionPrototypeId,
        ActionsContainerComponent? comp = null)
    {
        action = null;

        DebugTools.Assert(comp == null || comp.Owner == uid);
        comp ??= EnsureComp<ActionsContainerComponent>(uid);

        if (Exists(actionId))
        {
            if (!comp.Container.Contains(actionId.Value))
            {
                Log.Error($"Action {ToPrettyString(actionId.Value)} is not contained in the expected container {ToPrettyString(uid)}");
                return false;
            }

            if (!_actions.TryGetActionData(actionId, out action))
                return false;

            DebugTools.Assert(Transform(actionId.Value).ParentUid == uid);
            DebugTools.Assert(_container.IsEntityInContainer(actionId.Value));
            DebugTools.Assert(action.Container == uid);
            return true;
        }

        // Null prototypes are never valid entities, they mean that someone didn't provide a proper prototype.
        if (actionPrototypeId == null)
            return false;

        // Client cannot predict entity spawning.
        if (_netMan.IsClient && !IsClientSide(uid))
            return false;

        actionId = Spawn(actionPrototypeId);
        if (AddAction(uid, actionId.Value, action, comp) && _actions.TryGetActionData(actionId, out action))
            return true;

        Del(actionId.Value);
        actionId = null;
        return false;
    }

    /// <summary>
    /// Adds a pre-existing action to an action container.
    /// </summary>
    public bool AddAction(EntityUid uid, EntityUid actionId, BaseActionComponent? action = null, ActionsContainerComponent? comp = null)
    {
        if (!_actions.ResolveActionData(actionId, ref action))
            return false;

        if (action.Container != null)
        {
            Log.Error($"Attempted to insert an action {ToPrettyString(actionId)} that was already in a container {ToPrettyString(action.Container.Value)}");
            return false;
        }

        DebugTools.Assert(comp == null || comp.Owner == uid);
        comp ??= EnsureComp<ActionsContainerComponent>(uid);
        if (!comp.Container.Insert(actionId))
        {
            Log.Error($"Failed to insert action {ToPrettyString(actionId)} into {ToPrettyString(uid)}");
            return false;
        }

        // Container insert events should have updated the component's fields:
        DebugTools.Assert(comp.Container.Contains(actionId));
        DebugTools.Assert(action.Container == uid);

        return true;
    }

    private void OnInit(EntityUid uid, ActionsContainerComponent component, ComponentInit args)
    {
        component.Container = _container.EnsureContainer<Container>(uid, ActionsContainerComponent.ContainerId);
    }

    private void OnShutdown(EntityUid uid, ActionsContainerComponent component, ComponentShutdown args)
    {
        component.Container.Shutdown();
    }

    private void OnEntityInserted(EntityUid uid, ActionsContainerComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ActionsContainerComponent.ContainerId)
            return;

        if (!_actions.TryGetActionData(args.Entity, out var data))
            return;

        DebugTools.Assert(data.AttachedEntity == null || data.Container != EntityUid.Invalid);
        DebugTools.Assert(data.Container == null || data.Container == uid);

        data.Container = uid;
        Dirty(uid, component);

        var ev = new ActionAddedEvent(args.Entity, data);
        RaiseLocalEvent(uid, ref ev);
    }

    private void OnEntityRemoved(EntityUid uid, ActionsContainerComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ActionsContainerComponent.ContainerId)
            return;

        // Actions should only be getting removed while terminating or moving outside of PVS range.
        DebugTools.Assert(Terminating(args.Entity)
                          || _netMan.IsServer // I love gibbing code
                          || _timing.ApplyingState);

        if (!_actions.TryGetActionData(args.Entity, out var data, false))
            return;

        // No event - the only entity that should care about this is the entity that the action was provided to.
        if (data.AttachedEntity != null)
            _actions.RemoveAction(data.AttachedEntity.Value, args.Entity, null, data);

        var ev = new ActionRemovedEvent(args.Entity, data);
        RaiseLocalEvent(uid, ref ev);

        if (_netMan.IsServer)
        {
            // TODO Actions
            // log an error or warning here once gibbing code is fixed.
            QueueDel(args.Entity);
        }
    }
}

/// <summary>
/// Raised directed at an action container when a new action entity gets inserted.
/// </summary>
[ByRefEvent]
public readonly struct ActionAddedEvent
{
    public readonly EntityUid Action;
    public readonly BaseActionComponent Component;

    public ActionAddedEvent(EntityUid action, BaseActionComponent component)
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
    public readonly BaseActionComponent Component;

    public ActionRemovedEvent(EntityUid action, BaseActionComponent component)
    {
        Action = action;
        Component = component;
    }
}
