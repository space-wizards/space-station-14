using Content.Server.Administration;
using Content.Server.Radiation.Components;
using Content.Shared.Administration;
using Content.Shared.Radiation.Events;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

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
        RaiseNetworkEvent(ev, session.Channel);
    }

    /// <summary>
    ///     Send new information for radiation overlay.
    /// </summary>
    private void UpdateDebugOverlay(EntityEventArgs ev)
    {
        foreach (var session in _debugSessions)
        {
            if (session.Status != SessionStatus.InGame)
                _debugSessions.Remove(session);
            else
                RaiseNetworkEvent(ev, session);
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

    private void UpdateGridcastDebugOverlay(
        double elapsedTime,
        int totalSources,
        int totalReceivers,
        List<DebugRadiationRay>? rays)
    {
        if (_debugSessions.Count == 0)
            return;

        var ev = new OnRadiationOverlayUpdateEvent(elapsedTime, totalSources, totalReceivers, rays ?? new());
        UpdateDebugOverlay(ev);
    }
}

/// <summary>
///     Toggle visibility of radiation rays coming from rad sources.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class RadiationViewCommand : LocalizedEntityCommands
{
    [Dependency] private readonly RadiationSystem _radiation = default!;

    public override string Command => "showradiation";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var session = shell.Player;
        if (session == null)
            return;

        _radiation.ToggleDebugView(session);
    }
}
