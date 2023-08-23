using Content.Server.Shuttles.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;
using Robust.Shared.Physics.Components;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems;

public sealed partial class DockingSystem
{
    private void UpdateAutodock()
    {
        // Work out what we can autodock with, what we shouldn't, and when we should stop tracking.
        // Autodocking only stops when the client closes that dock viewport OR they lose pilotcomponent.
        var dockingQuery = GetEntityQuery<DockingComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var recentQuery = GetEntityQuery<RecentlyDockedComponent>();
        var query = EntityQueryEnumerator<AutoDockComponent>();

        while (query.MoveNext(out var dockUid, out var comp))
        {
            if (comp.Requesters.Count == 0 || !dockingQuery.TryGetComponent(dockUid, out var dock))
            {
                RemComp<AutoDockComponent>(dockUid);
                continue;
            }

            // Don't re-dock if we're already docked or recently were.
            if (dock.Docked || recentQuery.HasComponent(dockUid))
                continue;

            var dockable = GetDockable(dockUid, xformQuery.GetComponent(dockUid));

            if (dockable == null)
                continue;

            TryDock(dockUid, dock, dockable.Owner, dockable);
        }

        // Work out recent docks that have gone past their designated threshold.
        var checkedRecent = new HashSet<EntityUid>();
        var recentQueryEnumerator = EntityQueryEnumerator<RecentlyDockedComponent, TransformComponent>();

        while (recentQueryEnumerator.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!checkedRecent.Add(uid))
                continue;

            if (!dockingQuery.HasComponent(uid))
            {
                RemCompDeferred<RecentlyDockedComponent>(uid);
                continue;
            }

            if (!xformQuery.TryGetComponent(comp.LastDocked, out var otherXform))
            {
                RemCompDeferred<RecentlyDockedComponent>(uid);
                continue;
            }

            var worldPos = _transform.GetWorldPosition(xform, xformQuery);
            var otherWorldPos = _transform.GetWorldPosition(otherXform, xformQuery);

            if ((worldPos - otherWorldPos).Length() < comp.Radius)
                continue;

            Log.Debug($"Removed RecentlyDocked from {ToPrettyString(uid)} and {ToPrettyString(comp.LastDocked)}");
            RemComp<RecentlyDockedComponent>(uid);
            RemComp<RecentlyDockedComponent>(comp.LastDocked);
        }
    }

    private void OnRequestUndock(EntityUid uid, ShuttleConsoleComponent component, UndockRequestMessage args)
    {
        var dork = GetEntity(args.DockEntity);

        Log.Debug($"Received undock request for {ToPrettyString(dork)}");

        // TODO: Validation
        if (!TryComp<DockingComponent>(dork, out var dock) ||
            !dock.Docked ||
            HasComp<PreventPilotComponent>(Transform(uid).GridUid))
        {
            return;
        }

        Undock(dork, dock);
    }

    private void OnRequestAutodock(EntityUid uid, ShuttleConsoleComponent component, AutodockRequestMessage args)
    {
        var dork = GetEntity(args.DockEntity);
        Log.Debug($"Received autodock request for {ToPrettyString(dork)}");
        var player = args.Session.AttachedEntity;

        if (player == null ||
            !HasComp<DockingComponent>(dork) ||
            HasComp<PreventPilotComponent>(Transform(uid).GridUid))
        {
            return;
        }

        // TODO: Validation
        var comp = EnsureComp<AutoDockComponent>(dork);
        comp.Requesters.Add(player.Value);
    }

    private void OnRequestStopAutodock(EntityUid uid, ShuttleConsoleComponent component, StopAutodockRequestMessage args)
    {
        var dork = GetEntity(args.DockEntity);
        Log.Debug($"Received stop autodock request for {ToPrettyString(dork)}");

        var player = args.Session.AttachedEntity;

        // TODO: Validation
        if (player == null || !TryComp<AutoDockComponent>(dork, out var comp)) return;

        comp.Requesters.Remove(player.Value);

        if (comp.Requesters.Count == 0)
            RemComp<AutoDockComponent>(dork);
    }
}
