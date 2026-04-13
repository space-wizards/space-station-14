using Content.Server.Administration;
using Content.Server.Administration.AuditLog;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands;

[AdminCommand(AdminFlags.Round)]
public sealed class EndRoundCommand : LocalizedEntityCommands
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IAdminAuditLogManager _auditLog = default!;

    public override string Command => "endround";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_gameTicker.RunLevel != GameRunLevel.InRound)
        {
            shell.WriteLine(Loc.GetString("shell-can-only-run-while-round-is-active"));
            return;
        }

        if (shell.Player != null)
        {
            _auditLog.LogAction(
                shell.Player.UserId.UserId,
                AdminAuditAction.EndRound,
                AuditSeverity.Critical,
                "Ended round");
        }

        _gameTicker.EndRound();
    }
}
