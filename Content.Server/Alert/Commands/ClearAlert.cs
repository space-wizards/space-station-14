using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Administration;
using Content.Shared.Alert;
using Robust.Shared.Console;

namespace Content.Server.Alert.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class ClearAlert : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _e = default!;

        public override string Command => "clearalert";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player?.AttachedEntity == null)
            {
                shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
                return;
            }

            var attachedEntity = player.AttachedEntity.Value;

            if (args.Length > 1)
            {
                var target = args[1];
                if (!CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (!_e.TryGetComponent(attachedEntity, out AlertsComponent? alertsComponent))
            {
                shell.WriteLine(Loc.GetString("cmd-clearalert-no-alerts-component"));
                return;
            }

            var alertType = args[0];
            var alertsSystem = _e.System<AlertsSystem>();
            if (!alertsSystem.TryGet(alertType, out var alert))
            {
                shell.WriteLine(Loc.GetString("cmd-clearalert-unrecognized-alert-type", ("alertType", alertType)));
                return;
            }

            alertsSystem.ClearAlert(attachedEntity, alert.ID);
        }
    }
}
