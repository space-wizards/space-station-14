using Content.Shared.Administration.Logs;
using Content.Shared.BroadcastInteractionUsingToContainer.Components;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Shared.BroadcastInteractionUsingToContainer.Systems;

/// <summary>
/// Provides broadcasting interaction from entity to entities in it's container
/// </summary>
public sealed partial class BroadcastInteractUsingToContainerSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BroadcastInteractUsingToContainerComponent, BeforeRangedInteractEvent>(OnBeforeInteractUsing);
        SubscribeLocalEvent<BroadcastInteractUsingTargetToContainerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<BroadcastAfterInteractUsingToContainerComponent, AfterInteractEvent>(OnAfterInteractUsing);
    }

    private void OnBeforeInteractUsing(Entity<BroadcastInteractUsingToContainerComponent> entity, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled)
            return;

        EventWrapper wrapEvent = new(args);

        BroadcastToContainedEntities((entity.Owner, entity.Comp), ref wrapEvent, (containedUid, user, used, target, clickLocation, canReach) =>
        {
            if (IsDeleted(user) || IsDeleted(used) || IsDeleted(target))
                return false;

            _adminLog.Add(LogType.InteractUsing, LogImpact.Low,
            $"{ToPrettyString(user):user} interacted with {ToPrettyString(target):target} using {ToPrettyString(containedUid):used} which was in container of {ToPrettyString(used)}");

            //changed used -> containedUid
            var interactEvent = new InteractBeforeUsingWithInContainerEvent(user, containedUid, target, clickLocation, canReach);
            RaiseLocalEvent(containedUid, interactEvent, true);

            return TryBroadcastToEntitiesInsideTargetContainers(wrapEvent, containedUid);
        });
        // Dont forget to hand over Handled to origin event
        args.Handled = wrapEvent.Handled;
    }

    private void OnInteractUsing(Entity<BroadcastInteractUsingTargetToContainerComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        EventWrapper wrapEvent = new(args);
        args.Handled = OnInteractUsing(entity, ref wrapEvent);
    }

    private bool OnInteractUsing(Entity<BroadcastInteractUsingTargetToContainerComponent> entity, ref EventWrapper args)
    {
        // Dont forget to swap target and used cause know we broadcast to target's contained entities
        BroadcastToContainedEntities((entity.Owner, entity.Comp), ref args, (containedUid, user, used, target, clickLocation, canReach) =>
        {
            // check used for null cause in EventWrapper it could be null
            if (IsDeleted(user) || IsDeleted(used) || IsDeleted(target) || target == null)
                return false;

            _adminLog.Add(LogType.InteractUsing, LogImpact.Low,
            $"{ToPrettyString(user):user} interacted with {ToPrettyString(containedUid):target} from the container of {ToPrettyString(target):target} using {ToPrettyString(used):used}");

            //changed target -> containedUid
            var interactEvent = new InteractUsingTargetInContainerEvent(user, used, containedUid, clickLocation, canReach);
            RaiseLocalEvent(used, interactEvent, true);
            return interactEvent.Handled;
        });
        // Dont forget to hand over Handled to origin event
        return args.Handled;
    }

    private void OnAfterInteractUsing(Entity<BroadcastAfterInteractUsingToContainerComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        EventWrapper wrapEvent = new(args);

        BroadcastToContainedEntities((entity.Owner, entity.Comp), ref wrapEvent, (containedUid, user, used, target, clickLocation, canReach) =>
        {
            if (IsDeleted(user) || IsDeleted(used) || IsDeleted(target))
                return false;

            _adminLog.Add(LogType.InteractUsing, LogImpact.Low,
            $"{ToPrettyString(user):user} interacted with {ToPrettyString(target):target} using {ToPrettyString(containedUid):used} which was in container of {ToPrettyString(used)}");

            // changed used -> containedUid
            var interactEvent = new InteractAfterUsingWithInContainerEvent(user, containedUid, target, clickLocation, canReach);
            RaiseLocalEvent(containedUid, interactEvent, true);

            return TryBroadcastToEntitiesInsideTargetContainers(wrapEvent, containedUid);
        });
        // Dont forget to hand over Handled to origin event
        args.Handled = wrapEvent.Handled;
    }

    private bool BroadcastToContainedEntities(Entity<BroadcastUsingToContainerComponent> entity, ref EventWrapper args,
                    Func<EntityUid, EntityUid, EntityUid, EntityUid?, EntityCoordinates, bool, bool> eventBroadcaster)
    {
        var handled = false;
        var (uid, comp) = entity;
        var containers = _container.GetAllContainers(uid);

        foreach (var container in containers)
        {
            foreach (var containedUid in container.ContainedEntities)
            {
                if (_entityWhitelist.IsWhitelistFail(comp.Whitelist, containedUid)
                    || _entityWhitelist.IsBlacklistPass(comp.Blacklist, containedUid))
                    continue;

                if (eventBroadcaster(containedUid, args.User, args.Used, args.Target, args.ClickLocation, args.CanReach))
                    args.Handled = true;
            }
        }
        return handled;
    }

    /// <summary>
    /// Checks if <paramref name="args.Target"/> has <see cref="BroadcastInteractUsingTargetToContainerComponent"/> and if so broadcasts events.
    /// </summary>
    private bool TryBroadcastToEntitiesInsideTargetContainers(EventWrapper args, EntityUid used)
    {
        if (args.Handled || args.Target == null
            || !TryComp<BroadcastInteractUsingTargetToContainerComponent>(args.Target, out var broadcastTarget))
            return args.Handled;

        EventWrapper wrapEvent = new(args.User, used, args.Target, args.ClickLocation, args.CanReach, args.Handled);
        OnInteractUsing((args.Target.Value, broadcastTarget), ref wrapEvent);
        return wrapEvent.Handled;
    }

    private bool IsDeleted(EntityUid uid)
    {
        return TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid);
    }

    private bool IsDeleted(EntityUid? uid)
    {
        if (uid.HasValue)
            return IsDeleted(uid.Value);

        return false;
    }
}
