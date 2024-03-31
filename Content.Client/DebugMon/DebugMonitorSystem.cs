using Content.Client.Administration.Managers;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;


namespace Content.Client.DebugMon;

/// <summary>
/// This handles preventing certain debug monitors from appearing.
/// </summary>
public sealed class DebugMonitorSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterface = default!;

    public override void FrameUpdate(float frameTime)
    {
        if (!_admin.IsActive() && _cfg.GetCVar(CCVars.DebugCoordinatesAdminOnly))
            _userInterface.DebugMonitors.SetMonitor(DebugMonitor.Coords, false);
    }
}
