using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Content.Shared.CCVar;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    private void InitializeGridFills()
    {
        SubscribeLocalEvent<GridFillComponent, MapInitEvent>(OnGridFillMapInit);
    }

    private void OnGridFillMapInit(EntityUid uid, GridFillComponent component, MapInitEvent args)
    {
        if (!TryComp<DockingComponent>(uid, out var dock) ||
            !TryComp<TransformComponent>(uid, out var xform) ||
            xform.GridUid == null)
        {
            return;
        }

        if (_cfg.GetCVar(CCVars.DisableGridFill))
            return;

        // Spawn on a dummy map and try to dock if possible, otherwise dump it.
        var mapId = _mapManager.CreateMap();
        var valid = false;

        if (_loader.TryLoad(mapId, component.Path.ToString(), out var ent) &&
            ent.Count == 1 &&
            TryComp<TransformComponent>(ent[0], out var shuttleXform))
        {
            var escape = GetSingleDock(ent[0]);

            if (escape != null)
            {
                var config = _dockSystem.GetDockingConfig(ent[0], xform.GridUid.Value, escape.Value.Entity, escape.Value.Component, uid, dock);

                if (config != null)
                {
                    FTLDock(config, shuttleXform);

                    if (TryComp<StationMemberComponent>(xform.GridUid, out var stationMember))
                    {
                        _station.AddGridToStation(stationMember.Station, ent[0]);
                    }

                    valid = true;
                }
            }
        }

        if (!valid)
        {
            _sawmill.Error($"Error loading gridfill dock for {ToPrettyString(uid)} / {component.Path}");
        }

        _mapManager.DeleteMap(mapId);
    }

    private (EntityUid Entity, DockingComponent Component)? GetSingleDock(EntityUid uid)
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
