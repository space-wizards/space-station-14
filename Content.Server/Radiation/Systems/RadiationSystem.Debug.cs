using System.Linq;
using Content.Server.Administration;
using Content.Server.Radiation.Components;
using Content.Shared.Administration;
using Content.Shared.Radiation.Events;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Players;

namespace Content.Server.Radiation.Systems;

// radiation overlay debug logic
// rad rays send only to clients that enabled debug overlay
public partial class RadiationSystem
{
    private readonly HashSet<ICommonSession> _debugSessions = new();

    /// <summary>
    ///     Toggle radiation debug overlay for selected player.
    /// </summary>
    public void ToggleDebugView(ICommonSession session)
    {
        bool isEnabled;
        if (_debugSessions.Add(session))
        {
            isEnabled = true;
        }
        else
        {
            _debugSessions.Remove(session);
            isEnabled = false;
        }

        var ev = new OnRadiationOverlayToggledEvent(isEnabled);
        RaiseNetworkEvent(ev, session.ConnectedClient);
    }

    /// <summary>
    ///     Send new information for radiation overlay.
    /// </summary>
    private void UpdateDebugOverlay(EntityEventArgs ev)
    {
        var sessions = _debugSessions.ToArray();
        foreach (var session in sessions)
        {
            if (session.Status != SessionStatus.InGame)
                _debugSessions.Remove(session);
            RaiseNetworkEvent(ev, session.ConnectedClient);
        }
    }

    private void UpdateResistanceDebugOverlay()
    {
        if (_debugSessions.Count == 0)
            return;

        var dict = new Dictionary<NetEntity, Dictionary<Vector2i, float>>();

        var gridQuery = AllEntityQuery<MapGridComponent, RadiationGridResistanceComponent>();

        while (gridQuery.MoveNext(out var gridUid, out _, out var resistance))
        {
            var resMap = resistance.ResistancePerTile;
            dict.Add(GetNetEntity(gridUid), resMap);
        }

        var ev = new OnRadiationOverlayResistanceUpdateEvent(dict);
        UpdateDebugOverlay(ev);
    }

    private void UpdateGridcastDebugOverlay(double elapsedTime, int totalSources,
        int totalReceivers, List<RadiationRay> rays)
    {
        if (_debugSessions.Count == 0)
            return;

        var ev = new OnRadiationOverlayUpdateEvent(elapsedTime, totalSources, totalReceivers, rays);
        UpdateDebugOverlay(ev);
    }
}

/// <summary>
///     Toggle visibility of radiation rays coming from rad sources.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class RadiationViewCommand : IConsoleCommand
{
    public string Command => "showradiation";
    public string Description => Loc.GetString("radiation-command-description");
    public string Help => Loc.GetString("radiation-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var session = shell.Player;
        if (session == null)
            return;

        var entityManager = IoCManager.Resolve<IEntityManager>();
        entityManager.System<RadiationSystem>().ToggleDebugView(session);
    }
}
