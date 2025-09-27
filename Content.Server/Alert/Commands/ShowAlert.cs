using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Administration;
using Content.Shared.Alert;
using Robust.Shared.Console;

namespace Content.Server.Alert.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class ShowAlert : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _e = default!;

        public string Command => "showalert";
        public string Description => Loc.GetString("cmd-showalert-desc");
        public string Help => Loc.GetString("cmd-showalert-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player?.AttachedEntity == null)
            {
                shell.WriteLine(Loc.GetString("cmd-showalert-server-no-entity"));
                return;
            }

            var attachedEntity = player.AttachedEntity.Value;

            if (args.Length > 2)
            {
                var target = args[2];
                if (!CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (!_e.TryGetComponent(attachedEntity, out AlertsComponent? alertsComponent))
            {
                shell.WriteLine(Loc.GetString("cmd-showalert-user-no-alerts"));
                return;
            }

            var alertType = args[0];
            var severity = args[1];
            var alertsSystem = _e.System<AlertsSystem>();
            if (!alertsSystem.TryGet(alertType, out var alert))
            {
                shell.WriteLine(Loc.GetString("cmd-showalert-unrecognized-type", ("type", alertType)));
                return;
            }
            if (!short.TryParse(severity, out var sevint))
            {
                shell.WriteLine(Loc.GetString("cmd-showalert-invalid-severity", ("severity", sevint)));
                return;
            }

            short? severity1 = sevint == -1 ? null : sevint;
            alertsSystem.ShowAlert(attachedEntity, alert.ID, severity1, null);
        }
    }
}
