using Content.Client.Administration.Managers;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;


namespace Content.Client.DebugMon;

/// <summary>
/// This handles preventing certain debug monitors from appearing.
/// </summary>
public sealed partial class DebugMonitorSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IClientAdminManager _admin = default!;
    [Dependency] private IUserInterfaceManager _userInterface = default!;

    public override void FrameUpdate(float frameTime)
    {
        if (!_admin.IsActive() && _cfg.GetCVar(CCVars.DebugCoordinatesAdminOnly))
            _userInterface.DebugMonitors.SetMonitor(DebugMonitor.Coords, false);
        else
            _userInterface.DebugMonitors.SetMonitor(DebugMonitor.Coords, true);
    }
}
