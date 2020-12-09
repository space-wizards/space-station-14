#nullable enable
using System;
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Administration;
using Content.Shared.Alert;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Alerts
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class ShowAlert : IClientCommand
    {
        public string Command => "showalert";
        public string Description => "Shows an alert for a player, defaulting to current player";
        public string Help => "showalert <alertType> <severity, -1 if no severity> <name or userID, omit for current player>";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player == null)
            {
                shell.SendText(player, "You cannot run this command from the server.");
                return;
            }

            var attachedEntity = player.AttachedEntity;

            if (attachedEntity == null)
            {
                shell.SendText(player, "You don't have an entity.");
                return;
            }

            if (args.Length > 2)
            {
                var target = args[2];
                if (!CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (!CommandUtils.ValidateAttachedEntity(shell, player, attachedEntity))
                return;

            if (!attachedEntity.TryGetComponent(out ServerAlertsComponent? alertsComponent))
            {
                shell.SendText(player, "user has no alerts component");
                return;
            }

            var alertType = args[0];
            var severity = args[1];
            var alertMgr = IoCManager.Resolve<AlertManager>();
            if (!alertMgr.TryGet(Enum.Parse<AlertType>(alertType), out var alert))
            {
                shell.SendText(player, "unrecognized alertType " + alertType);
                return;
            }
            if (!short.TryParse(severity, out var sevint))
            {
                shell.SendText(player, "invalid severity " + sevint);
                return;
            }
            alertsComponent.ShowAlert(alert.AlertType, sevint == -1 ? (short?) null : sevint);
        }
    }
}
