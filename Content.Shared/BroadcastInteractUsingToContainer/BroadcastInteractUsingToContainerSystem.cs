using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Shared.BroadcastInteractionUsingToContainer;

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

        SubscribeLocalEvent<BroadcastInteractUsingToContainerComponent, BeforeRangedInteractEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<BroadcastInteractUsingToContainerComponent> entity, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach)
            return;

        if (args.Target == null)
            return;

        var (uid, comp) = entity;
        var containers = _container.GetAllContainers(uid);
        foreach (var container in containers)
        {
            foreach (var containedUid in container.ContainedEntities)
            {
                if (_entityWhitelist.IsWhitelistFail(comp.Whitelist, containedUid)
                    || _entityWhitelist.IsBlacklistPass(comp.Blacklist, containedUid))
                    continue;

                if (RaiseInteractionInContainerEvent(args.User, containedUid, args.Target.Value, args.ClickLocation))
                    args.Handled = true;
            }
        }
    }

    private bool RaiseInteractionInContainerEvent(EntityUid user, EntityUid used, EntityUid target, EntityCoordinates clickLocation)
    {
        if (IsDeleted(user) || IsDeleted(used) || IsDeleted(target))
            return false;

        _adminLog.Add(LogType.InteractUsing, LogImpact.Low,
            $"{ToPrettyString(user):user} interacted with {ToPrettyString(target):target} using {ToPrettyString(used):used} which was in container");

        var interactUsingInContainerEvent = new InteractUsingInContainerEvent(user, used, target, clickLocation);
        RaiseLocalEvent(used, interactUsingInContainerEvent, true);
        return interactUsingInContainerEvent.Handled;
    }

    private bool IsDeleted(EntityUid uid)
    {
        return TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid);
    }
}
