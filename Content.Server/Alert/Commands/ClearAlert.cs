using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Administration;
using Content.Shared.Alert;
using Robust.Shared.Console;

namespace Content.Server.Alert.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class ClearAlert : LocalizedEntityCommands
    {
        [Dependency] private readonly AlertsSystem _alertSys = default!;

        public override string Command => "clearalert";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player?.AttachedEntity == null)
            {
                shell.WriteLine(Loc.GetString($"shell-must-be-attached-to-entity"));
                return;
            }

            var attachedEntity = player.AttachedEntity.Value;

            if (args.Length > 1)
            {
                var target = args[1];
                if (!CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity))
                    return;
            }

            if (!EntityManager.TryGetComponent(attachedEntity, out AlertsComponent? alertsComponent))
            {
                shell.WriteLine(Loc.GetString($"shell-entity-target-lacks-component", ("componentName", nameof(AlertsComponent))));
                return;
            }

            var alertType = args[0];
            if (!_alertSys.TryGet(alertType, out var alert))
            {
                shell.WriteLine(Loc.GetString($"cmd-clearalert-unrecognized-alert", ("alert", alertType)));
                return;
            }

            _alertSys.ClearAlert(attachedEntity, alert.ID);
        }
    }
}
