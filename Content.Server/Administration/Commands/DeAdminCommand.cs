using Content.Server.Administration.Managers;
using Content.Server.Commands;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[UsedImplicitly]
[AdminCommand(AdminFlags.None)]
public sealed class DeAdminCommand : LocalizedCommands
{
    [Dependency] private readonly IAdminManager _admin = default!;

    public override string Command => "deadmin";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (CommandChecks.MustNotBeServer(shell, out var player))
            _admin.DeAdmin(player);
    }
}
