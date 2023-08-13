using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Toolshed;

namespace Content.Server.Administration.Toolshed;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class AdminsCommand : ToolshedCommand
{
    [Dependency] private readonly IAdminManager _admin = default!;

    [CommandImplementation("active")]
    public IEnumerable<IPlayerSession> Active()
    {
        return _admin.ActiveAdmins;
    }

    [CommandImplementation("all")]
    public IEnumerable<IPlayerSession> All()
    {
        return _admin.AllAdmins;
    }
}
