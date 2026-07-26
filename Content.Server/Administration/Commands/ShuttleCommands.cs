using System.Text.Json;
using Content.Server.Administration.AuditLog;
using Content.Server.Administration.AuditLog.Payloads;
using Content.Server.Administration.Logs;
using Content.Server.RoundEnd;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Localizations;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Round)]
    public sealed partial class CallShuttleCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
        [Dependency] private readonly IAdminAuditLogManager _auditLog = default!;

        public override string Command => "callshuttle";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (args.Length == 1 && TimeSpan.TryParseExact(args[0], ContentLocalizationManager.TimeSpanMinutesFormats, LocalizationManager.DefaultCulture, out var timeSpan))
            {
                if (shell.Player != null)
                {
                    _auditLog.LogAction(
                        shell.Player.UserId.UserId,
                        AdminAuditAction.CallShuttle,
                        AuditSeverity.Critical,
                        $"Called emergency shuttle with custom timer {timeSpan}",
                        payload: JsonSerializer.SerializeToDocument(
                            new AuditRoundActionPayload(shell.Player.UserId.UserId, "CallShuttle"),
                            AdminLogJsonOptions.Minimal));
                }

                _roundEndSystem.RequestRoundEnd(timeSpan, shell.Player?.AttachedEntity, checkCooldown: false);
            }

            else if (args.Length == 1)
                shell.WriteLine(Loc.GetString("shell-timespan-minutes-must-be-correct"));

            else
            {
                if (shell.Player != null)
                {
                    _auditLog.LogAction(
                        shell.Player.UserId.UserId,
                        AdminAuditAction.CallShuttle,
                        AuditSeverity.Critical,
                        "Called emergency shuttle",
                        payload: JsonSerializer.SerializeToDocument(
                            new AuditRoundActionPayload(shell.Player.UserId.UserId, "CallShuttle"),
                            AdminLogJsonOptions.Minimal));
                }

                _roundEndSystem.RequestRoundEnd(shell.Player?.AttachedEntity, checkCooldown: false);
            }
        }
    }

    [AdminCommand(AdminFlags.Round)]
    public sealed partial class RecallShuttleCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
        [Dependency] private readonly IAdminAuditLogManager _auditLog = default!;

        public override string Command => "recallshuttle";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player != null)
            {
                _auditLog.LogAction(
                    shell.Player.UserId.UserId,
                    AdminAuditAction.RecallShuttle,
                    AuditSeverity.Critical,
                    "Recalled emergency shuttle",
                    payload: JsonSerializer.SerializeToDocument(
                        new AuditRoundActionPayload(shell.Player.UserId.UserId, "RecallShuttle"),
                        AdminLogJsonOptions.Minimal));
            }

            _roundEndSystem.CancelRoundEndCountdown(shell.Player?.AttachedEntity, forceRecall: true);
        }
    }
}
