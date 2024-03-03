using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Station.Components;
using Content.Shared.UserInterface;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleConsoleSystem
{
    /// <summary>
    /// Gets the drone console target if applicable otherwise returns itself.
    /// </summary>
    private EntityUid? GetDroneConsole(EntityUid consoleUid)
    {
        var getShuttleEv = new ConsoleShuttleEvent
        {
            Console = consoleUid,
        };

        RaiseLocalEvent(consoleUid, ref getShuttleEv);
        return getShuttleEv.Console;
    }

    /// <summary>
    /// Refreshes all drone console entities.
    /// </summary>
    public void RefreshDroneConsoles()
    {
        var query = AllEntityQuery<DroneConsoleComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Entity = GetShuttleConsole(uid, comp);
        }
    }

    private void OnDronePilotConsoleOpen(EntityUid uid, DroneConsoleComponent component, AfterActivatableUIOpenEvent args)
    {
        component.Entity = GetShuttleConsole(uid);
    }

    private void OnDronePilotConsoleClose(EntityUid uid, DroneConsoleComponent component, BoundUIClosedEvent args)
    {
        // Only if last person closed UI.
        if (!_ui.IsUiOpen(uid, args.UiKey))
            component.Entity = null;
    }

    private void OnCargoGetConsole(EntityUid uid, DroneConsoleComponent component, ref ConsoleShuttleEvent args)
    {
        args.Console = GetShuttleConsole(uid, component);
    }

    /// <summary>
    /// Gets the relevant shuttle console to proxy from the drone console.
    /// </summary>
    private EntityUid? GetShuttleConsole(EntityUid uid, DroneConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        var stationUid = _station.GetOwningStation(uid);

        if (stationUid == null)
            return null;

        // I know this sucks but needs device linking or something idunno
        var query = AllEntityQuery<ShuttleConsoleComponent, TransformComponent>();

        while (query.MoveNext(out var cUid, out _, out var xform))
        {
            if (xform.GridUid == null ||
                !TryComp<StationMemberComponent>(xform.GridUid, out var member) ||
                member.Station != stationUid)
            {
                continue;
            }

            foreach (var compType in component.Components.Values)
            {
                if (!HasComp(xform.GridUid, compType.Component.GetType()))
                    continue;

                return cUid;
            }
        }

        return null;
    }
}
