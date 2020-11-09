using System;
using Content.Server.Commands;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedAlertsComponent))]
    public sealed class ServerAlertsComponent : SharedAlertsComponent
    {

        protected override void Startup()
        {
            base.Startup();

            if (EntitySystem.TryGet<WeightlessSystem>(out var weightlessSystem))
            {
                weightlessSystem.AddAlert(this);
            }
            else
            {
                Logger.WarningS("alert", "weightlesssystem not found");
            }
        }

        public override void OnRemove()
        {
            if (EntitySystem.TryGet<WeightlessSystem>(out var weightlessSystem))
            {
                weightlessSystem.RemoveAlert(this);
            }
            else
            {
                Logger.WarningS("alert", "weightlesssystem not found");
            }

            base.OnRemove();
        }

        public override ComponentState GetComponentState()
        {
            return new AlertsComponentState(CreateAlertStatesArray());
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            switch (message)
            {
                case ClickAlertMessage msg:
                {
                    var player = session.AttachedEntity;

                    if (player != Owner)
                    {
                        break;
                    }

                    // TODO: Implement clicking other status effects in the HUD
                    if (AlertManager.TryDecode(msg.EncodedAlert, out var alert))
                    {
                        PerformAlertClickCallback(alert, player);
                    }
                    else
                    {
                        Logger.WarningS("alert", "unrecognized encoded alert {0}", msg.EncodedAlert);
                    }

                    break;
                }
            }
        }
    }

    public sealed class ShowAlert : IClientCommand
    {
        public string Command => "showalert";
        public string Description => "Shows an alert for a player, defaulting to current player";
        public string Help => "showalert <alertType> <severity, -1 if no severity> <name or userID, omit for current player>";
        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var attachedEntity = player.AttachedEntity;
            if (args.Length > 2)
            {
                var target = args[2];
                if (!Commands.CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (!CommandUtils.ValidateAttachedEntity(shell, player, attachedEntity)) return;


            if (!attachedEntity.TryGetComponent(out ServerAlertsComponent alertsComponent))
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

    public sealed class ClearAlert : IClientCommand
    {
        public string Command => "clearalert";
        public string Description => "Clears an alert for a player, defaulting to current player";
        public string Help => "clearalert <alertType> <name or userID, omit for current player>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var attachedEntity = player.AttachedEntity;
            if (args.Length > 1)
            {
                var target = args[1];
                if (!CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (!CommandUtils.ValidateAttachedEntity(shell, player, attachedEntity)) return;

            if (!attachedEntity.TryGetComponent(out ServerAlertsComponent alertsComponent))
            {
                shell.SendText(player, "user has no alerts component");
                return;
            }

            var alertType = args[0];
            var alertMgr = IoCManager.Resolve<AlertManager>();
            if (!alertMgr.TryGet(Enum.Parse<AlertType>(alertType), out var alert))
            {
                shell.SendText(player, "unrecognized alertType " + alertType);
                return;
            }

            alertsComponent.ClearAlert(alert.AlertType);
        }
    }
}
