using Content.Server.Shuttles.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;
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
        var pilotQuery = GetEntityQuery<PilotComponent>();

        foreach (var (comp, body) in EntityQuery<AutoDockComponent, PhysicsComponent>())
        {
            if (comp.Requesters.Count == 0 || !dockingQuery.TryGetComponent(comp.Owner, out var dock))
            {
                RemComp<AutoDockComponent>(comp.Owner);
                continue;
            }

            // Don't re-dock if we're already docked or recently were.
            if (dock.Docked || recentQuery.HasComponent(comp.Owner)) continue;

            var toRemoveEnts = new RemQueue<EntityUid>();

            // If anyone d/cs handle no message being sent I guess?
            foreach (var entity in comp.Requesters)
            {
                if (pilotQuery.HasComponent(entity)) continue;
                toRemoveEnts.Add(entity);
            }

            foreach (var toRemove in toRemoveEnts)
            {
                comp.Requesters.Remove(toRemove);
            }

            if (comp.Requesters.Count == 0) continue;

            var dockable = GetDockable(body, xformQuery.GetComponent(comp.Owner));

            if (dockable == null) continue;

            TryDock(dock, dockable);
        }

        // Work out recent docks that have gone past their designated threshold.
        var checkedRecent = new HashSet<EntityUid>();

        foreach (var (comp, xform) in EntityQuery<RecentlyDockedComponent, TransformComponent>())
        {
            if (!checkedRecent.Add(comp.Owner)) continue;

            if (!dockingQuery.TryGetComponent(comp.Owner, out var dock))
            {
                RemComp<RecentlyDockedComponent>(comp.Owner);
                continue;
            }

            if (!xformQuery.TryGetComponent(comp.LastDocked, out var otherXform))
            {
                RemComp<RecentlyDockedComponent>(comp.Owner);
                continue;
            }

            var worldPos = _transformSystem.GetWorldPosition(xform, xformQuery);
            var otherWorldPos = _transformSystem.GetWorldPosition(otherXform, xformQuery);

            if ((worldPos - otherWorldPos).Length < comp.Radius) continue;

            _sawmill.Debug($"Removed RecentlyDocked from {ToPrettyString(comp.Owner)} and {ToPrettyString(comp.LastDocked)}");
            RemComp<RecentlyDockedComponent>(comp.Owner);
            RemComp<RecentlyDockedComponent>(comp.LastDocked);
        }
    }

    private void OnRequestUndock(UndockRequestEvent msg, EntitySessionEventArgs args)
    {
        _sawmill.Debug($"Received undock request for {ToPrettyString(msg.Entity)}");

        // TODO: Validation
        if (!TryComp<DockingComponent>(msg.Entity, out var dock) ||
            !dock.Docked) return;

        Undock(dock);
    }

    private void OnRequestAutodock(AutodockRequestEvent msg, EntitySessionEventArgs args)
    {
        _sawmill.Debug($"Received autodock request for {ToPrettyString(msg.Entity)}");
        var player = args.SenderSession.AttachedEntity;

        if (player == null || !HasComp<DockingComponent>(msg.Entity)) return;

        // TODO: Validation
        var comp = EnsureComp<AutoDockComponent>(msg.Entity);
        comp.Requesters.Add(player.Value);
    }

    private void OnRequestStopAutodock(StopAutodockRequestEvent msg, EntitySessionEventArgs args)
    {
        _sawmill.Debug($"Received stop autodock request for {ToPrettyString(msg.Entity)}");

        var player = args.SenderSession.AttachedEntity;

        // TODO: Validation
        if (player == null || !TryComp<AutoDockComponent>(msg.Entity, out var comp)) return;

        comp.Requesters.Remove(player.Value);

        if (comp.Requesters.Count == 0)
            RemComp<AutoDockComponent>(msg.Entity);
    }
}
