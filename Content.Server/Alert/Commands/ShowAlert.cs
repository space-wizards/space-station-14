using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Administration;
using Content.Shared.Alert;
using Robust.Shared.Console;

namespace Content.Server.Alert.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class ShowAlert : LocalizedEntityCommands
    {
        [Dependency] private readonly AlertsSystem _alertSys = default!;

        public override  string Command => "showalert";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player?.AttachedEntity == null)
            {
                shell.WriteLine(Loc.GetString($"shell-must-be-attached-to-entity"));
                return;
            }

            var attachedEntity = player.AttachedEntity.Value;

            if (args.Length > 2)
            {
                var target = args[2];
                if (!CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (!EntityManager.TryGetComponent(attachedEntity, out AlertsComponent? alertsComponent))
            {
                shell.WriteLine(Loc.GetString($"shell-entity-target-lacks-component", ("componentName", nameof(AlertsComponent))));
                return;
            }

            var alertType = args[0];
            var severity = args[1];
            if (!_alertSys.TryGet(alertType, out var alert))
            {
                shell.WriteLine(Loc.GetString($"cmd-clearalert-unrecognized-alert", ("alert", alertType)));
                return;
            }
            if (!short.TryParse(severity, out var sevint))
            {
                shell.WriteLine(Loc.GetString($"cmd-showalert-invalid-severity", ("severity", sevint)));
                return;
            }

            short? severity1 = sevint == -1 ? null : sevint;
            _alertSys.ShowAlert(attachedEntity, alert.ID, severity1);
        }
    }
}
