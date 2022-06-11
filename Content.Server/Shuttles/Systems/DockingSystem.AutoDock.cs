using Content.Server.Shuttles.Components;
using Content.Shared.Shuttles.Events;

namespace Content.Server.Shuttles.Systems;

public sealed partial class DockingSystem
{
    private void UpdateAutodock()
    {
        // Work out what we can autodock with, what we shouldn't, and when we should stop tracking.
        var dockingQuery = GetEntityQuery<DockingComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var recentQuery = GetEntityQuery<RecentlyDockedComponent>();

        foreach (var (comp, body) in EntityQuery<AutoDockComponent, PhysicsComponent>())
        {
            if (!dockingQuery.TryGetComponent(comp.Owner, out var dock))
            {
                RemComp<AutoDockComponent>(comp.Owner);
                continue;
            }

            // Don't re-dock if we're already docked or recently were.
            if (dock.Docked || recentQuery.HasComponent(comp.Owner)) continue;

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

        if (!TryComp<DockingComponent>(msg.Entity, out var dock)) return;

        // TODO: Validation
        EnsureComp<AutoDockComponent>(msg.Entity);
    }

    private void OnRequestStopAutodock(StopAutodockRequestEvent msg, EntitySessionEventArgs args)
    {
        _sawmill.Debug($"Received stop autodock request for {ToPrettyString(msg.Entity)}");

        // TODO: Validation
        RemComp<AutoDockComponent>(msg.Entity);
    }
}
