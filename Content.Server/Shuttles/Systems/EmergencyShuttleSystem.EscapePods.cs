using Content.Server.Shuttles.Components;

namespace Content.Server.Shuttles.Systems;

public sealed partial class EmergencyShuttleSystem
{
    private void InitializeEscapePods()
    {
        SubscribeLocalEvent<EscapePodFillComponent, MapInitEvent>(OnEscapePodFillMapInit);
    }

    private void OnEscapePodFillMapInit(EntityUid uid, EscapePodFillComponent component, MapInitEvent args)
    {
        if (!TryComp<DockingComponent>(uid, out var dock) ||
            !TryComp<TransformComponent>(uid, out var xform) ||
            xform.GridUid == null)
        {
            return;
        }

        // Spawn on a dummy map and try to dock if possible, otherwise dump it.
        var mapId = _mapManager.CreateMap();
        var valid = false;

        if (_map.TryLoad(mapId, component.Path.ToString(), out var ent) &&
            ent.Count == 1 &&
            TryComp<TransformComponent>(ent[0], out var shuttleXform))
        {
            var escape = GetEscapePodDock(ent[0]);

            if (escape != null)
            {
                var config = _dock.GetDockingConfig(ent[0], xform.GridUid.Value, escape.Value.Entity, escape.Value.Component, uid, dock);

                if (config != null)
                {
                    _shuttle.FTLDock(config, shuttleXform);
                    valid = true;
                }
            }
        }

        if (!valid)
        {
            _sawmill.Error($"Error loading escape dock for {ToPrettyString(uid)} / {component.Path}");
        }

        _mapManager.DeleteMap(mapId);
    }

    private (EntityUid Entity, DockingComponent Component)? GetEscapePodDock(EntityUid uid)
    {
        var dockQuery = GetEntityQuery<DockingComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(uid);

        var rator = xform.ChildEnumerator;

        while (rator.MoveNext(out var child))
        {
            if (!dockQuery.TryGetComponent(child, out var dock))
                continue;

            return (child.Value, dock);
        }

        return null;
    }
}
