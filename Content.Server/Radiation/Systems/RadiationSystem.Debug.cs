using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Radiation.Events;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Players;

namespace Content.Server.Radiation.Systems;

// radiation overview logic
public partial class RadiationSystem
{
    private readonly HashSet<ICommonSession> _debugSessions = new();

    /// <summary>
    ///     Toggle radiation debug view for selected player.
    /// </summary>
    public void ToggleDebugView(ICommonSession session)
    {
        bool isEnabled;
        if (!_debugSessions.Contains(session))
        {
            _debugSessions.Add(session);
            isEnabled = true;
        }
        else
        {
            _debugSessions.Remove(session);
            isEnabled = false;
        }

        var ev = new OnRadiationViewToggledEvent(isEnabled);
        RaiseNetworkEvent(ev, session.ConnectedClient);
    }

    /// <summary>
    ///     Send new information for radiation view.
    /// </summary>
    private void UpdateDebugView(OnRadiationViewUpdateEvent ev)
    {
        var sessions = _debugSessions.ToArray();
        foreach (var session in sessions)
        {
            if (session.Status != SessionStatus.InGame)
                _debugSessions.Remove(session);
            RaiseNetworkEvent(ev, session.ConnectedClient);
        }
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
