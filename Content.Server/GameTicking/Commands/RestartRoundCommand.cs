using Content.Server.Administration;
using Content.Server.Administration.AuditLog;
using Content.Server.RoundEnd;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands;

[AdminCommand(AdminFlags.Round)]
public sealed class RestartRoundCommand : LocalizedEntityCommands
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly IAdminAuditLogManager _auditLog = default!;

    public override string Command => "restartround";

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
                AdminAuditAction.RestartRound,
                AuditSeverity.Critical,
                "Requested round restart");
        }

        _roundEndSystem.EndRound();
    }
}

[AdminCommand(AdminFlags.Round)]
public sealed class RestartRoundNowCommand : LocalizedEntityCommands
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IAdminAuditLogManager _auditLog = default!;

    public override string Command => "restartroundnow";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player != null)
        {
            _auditLog.LogAction(
                shell.Player.UserId.UserId,
                AdminAuditAction.RestartRound,
                AuditSeverity.Critical,
                "Restarted round immediately");
        }

        _gameTicker.RestartRound();
    }
}
