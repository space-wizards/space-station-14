using System.Text.Json;
using Content.Server.Administration.AuditLog;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [UsedImplicitly]
    [AdminCommand(AdminFlags.None)]
    public sealed class DeAdminCommand : LocalizedCommands
    {
        [Dependency] private readonly IAdminManager _admin = default!;
        [Dependency] private readonly IAdminAuditLogManager _auditLog = default!;

        public override string Command => "deadmin";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine(Loc.GetString($"shell-cannot-run-command-from-server"));
                return;
            }

            _admin.DeAdmin(player);

            _auditLog.LogAction(
                player.UserId.UserId,
                AdminAuditAction.DeAdmin,
                AuditSeverity.Notable,
                "De-adminned self",
                payload: JsonSerializer.SerializeToDocument(new
                {
                    action = "deadmin_self"
                }));
        }
    }
}
