using Content.Shared.Administration.Logs;
using Content.Shared.BroadcastInteractionUsingToContainer.Components;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

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

        SubscribeLocalEvent<BroadcastInteractingFromContainerComponent, BeforeRangedInteractEvent>(OnBeforeInteractUsing);
        SubscribeLocalEvent<BroadcastInteractingIntoContainerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<BroadcastAfterInteractingFromContainerComponent, AfterInteractEvent>(OnAfterInteractUsing);
    }

    private void OnBeforeInteractUsing(Entity<BroadcastInteractingFromContainerComponent> entity, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled)
            return;

        EventWrapper wrapEvent = new(args);

        GetAllowedContainedEntitiesAndBroadcast((entity.Owner, entity.Comp), ref wrapEvent, (EntityUid containedUid, ref EventWrapper wrapEvent) =>
        {
            //changed used -> containedUid
            var interactEvent = new InteractBeforeUsingFromContainerEvent(wrapEvent.User, containedUid, wrapEvent.Target,
                                                                            wrapEvent.ClickLocation, wrapEvent.CanReach);
            RaiseLocalEvent(containedUid, interactEvent, true);
            // we need to do it because in next method we need to know if already handled
            wrapEvent.Handled = interactEvent.Handled;

            if (wrapEvent.Handled)
            {
                _adminLog.Add(LogType.InteractUsing, LogImpact.Low,
                    $"{ToPrettyString(wrapEvent.User):user} interacted with {ToPrettyString(wrapEvent.Target):target} using {ToPrettyString(containedUid):used} which was in container of {ToPrettyString(wrapEvent.Used)}");
                return wrapEvent.Handled;
            }

            TryBroadcastToEntitiesInsideTargetContainers(ref wrapEvent, containedUid, out var broadcastedUid);

            if (wrapEvent.Handled)
                _adminLog.Add(LogType.InteractUsing, LogImpact.Low,
                    $"{ToPrettyString(wrapEvent.User):user} interacted with {ToPrettyString(broadcastedUid):target} which was in container of {ToPrettyString(wrapEvent.Target):target} using {ToPrettyString(containedUid):used} which was in container of {ToPrettyString(wrapEvent.Used)}");

            return wrapEvent.Handled;
        }, out _);
        // Dont forget to hand over Handled to origin event
        args.Handled = wrapEvent.Handled;
    }

    private void OnInteractUsing(Entity<BroadcastInteractingIntoContainerComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        EventWrapper wrapEvent = new(args);

        GetAllowedContainedEntitiesAndBroadcast((entity.Owner, entity.Comp), ref wrapEvent, (EntityUid containedUid, ref EventWrapper args) =>
        {
            // check used for null cause in EventWrapper it could be null
            if (IsDeleted(containedUid))
                return false;

            //changed target -> containedUid
            var interactEvent = new InteractUsedIntoContainerEvent(args.User, args.Used, containedUid, args.ClickLocation, args.CanReach);
            RaiseLocalEvent(args.Used, interactEvent, true);
            return interactEvent.Handled;
        }, out var broadcastedUid);

        if (wrapEvent.Handled)
            _adminLog.Add(LogType.InteractUsing, LogImpact.Low,
            $"{ToPrettyString(args.User):user} interacted with {ToPrettyString(broadcastedUid):target} from the container of {ToPrettyString(args.Target):target} using {ToPrettyString(args.Used):used}");
    }

    private void OnAfterInteractUsing(Entity<BroadcastAfterInteractingFromContainerComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        EventWrapper wrapEvent = new(args);

        GetAllowedContainedEntitiesAndBroadcast((entity.Owner, entity.Comp), ref wrapEvent, (EntityUid containedUid, ref EventWrapper args) =>
        {
            if (IsDeleted(containedUid))
                return false;

            // changed used -> containedUid
            var interactEvent = new InteractAfterUsingFromContainerEvent(args.User, containedUid, args.Target,
                                                                            args.ClickLocation, args.CanReach);
            RaiseLocalEvent(containedUid, interactEvent, true);
            // we need to do it because in next method we need to know if already handled
            wrapEvent.Handled = interactEvent.Handled;

            if (wrapEvent.Handled)
            {
                _adminLog.Add(LogType.InteractUsing, LogImpact.Low,
                    $"{ToPrettyString(wrapEvent.User):user} interacted with {ToPrettyString(wrapEvent.Target):target} using {ToPrettyString(containedUid):used} which was in container of {ToPrettyString(wrapEvent.Used)}");
                return wrapEvent.Handled;
            }

            TryBroadcastToEntitiesInsideTargetContainers(ref wrapEvent, containedUid, out var broadcastedUid);

            if (wrapEvent.Handled)
                _adminLog.Add(LogType.InteractUsing, LogImpact.Low,
                    $"{ToPrettyString(wrapEvent.User):user} interacted with {ToPrettyString(broadcastedUid):target} which was in container of {ToPrettyString(wrapEvent.Target):target} using {ToPrettyString(containedUid):used} which was in container of {ToPrettyString(wrapEvent.Used)}");

            return wrapEvent.Handled;
        }, out _);
        // Dont forget to hand over Handled to origin event
        args.Handled = wrapEvent.Handled;
    }

    /// <summary>
    /// Checks if <paramref name="args.Target"/> has <see cref="BroadcastInteractingIntoContainerComponent"/> and if so broadcasts events.
    /// </summary>
    private void TryBroadcastToEntitiesInsideTargetContainers(ref EventWrapper args, EntityUid used, out EntityUid? broadcastedUid)
    {
        broadcastedUid = null;
        if (args.Handled || args.Target == null
            || !TryComp<BroadcastInteractingIntoContainerComponent>(args.Target, out var broadcastTarget))
            return;

        EventWrapper wrapEvent = new(args.User, used, args.Target, args.ClickLocation, args.CanReach, args.Handled);
        // Yes, code the same as in OnInteractUsing, thats okay, in case we need to differ events.
        // Dont forget to swap target and used cause know we broadcast to target's contained entities
        GetAllowedContainedEntitiesAndBroadcast((args.Target.Value, broadcastTarget), ref wrapEvent, (EntityUid containedUid, ref EventWrapper args) =>
        {
            // check used for null cause in EventWrapper it could be null
            if (IsDeleted(containedUid))
                return false;

            //changed target -> containedUid
            var interactEvent = new InteractUsedIntoContainerEvent(args.User, args.Used, containedUid, args.ClickLocation, args.CanReach);
            RaiseLocalEvent(args.Used, interactEvent, true);
            return interactEvent.Handled;
        }, out broadcastedUid);
        //Dont forget to give Handle further
        args.Handled = wrapEvent.Handled;
    }

    private void GetAllowedContainedEntitiesAndBroadcast(Entity<BroadcastUsingToContainerComponent> entity, ref EventWrapper args,
                    EventBroadcaster eventBroadcaster, out EntityUid? broadcastedUid)
    {
        broadcastedUid = null;
        var (uid, comp) = entity;
        var containers = _container.GetAllContainers(uid);

        foreach (var container in containers)
        {
            foreach (var containedUid in container.ContainedEntities)
            {
                if (_entityWhitelist.IsWhitelistFail(comp.Whitelist, containedUid)
                    || _entityWhitelist.IsBlacklistPass(comp.Blacklist, containedUid))
                    continue;

                if (IsDeleted(containedUid))
                    continue;

                if (!eventBroadcaster(containedUid, ref args))
                    continue;

                broadcastedUid = containedUid;
                args.Handled = true;
                // On this break depends adminlogs
                break;
            }
        }
    }

    private delegate bool EventBroadcaster(EntityUid containedUid, ref EventWrapper args);

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
