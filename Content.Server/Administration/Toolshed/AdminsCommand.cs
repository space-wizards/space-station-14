using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;

namespace Content.Server.Administration.Toolshed;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class AdminsCommand : ToolshedCommand
{
    [Dependency] private readonly IAdminManager _admin = default!;

    [CommandImplementation("active")]
    public IEnumerable<ICommonSession> Active()
    {
        return _admin.ActiveAdmins;
    }

    [CommandImplementation("all")]
    public IEnumerable<ICommonSession> All()
    {
        return _admin.AllAdmins;
    }
}
