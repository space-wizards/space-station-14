#nullable enable
using System;
using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Administration;
using Content.Shared.Alert;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Content.Server.Alert.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class ShowAlert : IConsoleCommand
    {
        public string Command => "showalert";
        public string Description => "Shows an alert for a player, defaulting to current player";
        public string Help => "showalert <alertType> <severity, -1 if no severity> <name or userID, omit for current player>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("You cannot run this command from the server.");
                return;
            }

            var attachedEntity = player.AttachedEntity;

            if (attachedEntity == null)
            {
                shell.WriteLine("You don't have an entity.");
                return;
            }

            if (args.Length > 2)
            {
                var target = args[2];
                if (!CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (!attachedEntity.TryGetComponent(out ServerAlertsComponent? alertsComponent))
            {
                shell.WriteLine("user has no alerts component");
                return;
            }

            var alertType = args[0];
            var severity = args[1];
            var alertMgr = IoCManager.Resolve<AlertManager>();
            if (!alertMgr.TryGet(Enum.Parse<AlertType>(alertType), out var alert))
            {
                shell.WriteLine("unrecognized alertType " + alertType);
                return;
            }
            if (!short.TryParse(severity, out var sevint))
            {
                shell.WriteLine("invalid severity " + sevint);
                return;
            }
            alertsComponent.ShowAlert(alert.AlertType, sevint == -1 ? (short?) null : sevint);
        }
    }
}
