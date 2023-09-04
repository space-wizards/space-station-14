using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;

namespace Content.Server.Toolshed.Commands.AdminDebug;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
[InjectDependencies]
public sealed partial class ACmdCommand : ToolshedCommand
{
    [Dependency] private IAdminManager _adminManager = default!;

    [CommandImplementation("perms")]
    public AdminFlags[]? Perms([PipedArgument] CommandSpec command)
    {
        var res = _adminManager.TryGetCommandFlags(command, out var flags);
        if (res)
            flags ??= Array.Empty<AdminFlags>();
        return flags;
    }

    [CommandImplementation("caninvoke")]
    public bool CanInvoke(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] CommandSpec command,
        [CommandArgument] ValueRef<IPlayerSession> player
        )
    {
        // Deliberately discard the error.
        return ((IPermissionController) _adminManager).CheckInvokable(command, player.Evaluate(ctx), out var err);
    }
}
