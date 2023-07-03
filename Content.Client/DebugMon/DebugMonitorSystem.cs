using Content.Client.Administration.Managers;
using Robust.Client.UserInterface;

namespace Content.Client.DebugMon;

/// <summary>
/// This handles preventing certain debug monitors from appearing.
/// </summary>
public sealed class DebugMonitorSystem : EntitySystem
{
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterface = default!;

    public override void FrameUpdate(float frameTime)
    {
        if (_admin.IsActive())
            _userInterface.DebugMonitors.SetMonitor(DebugMonitor.Coords, true);
        else
            _userInterface.DebugMonitors.SetMonitor(DebugMonitor.Coords, false);
    }
}