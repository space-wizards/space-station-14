using System;
using Content.Server.Commands;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedActionsComponent))]
    public sealed class ServerActionsComponent : SharedActionsComponent
    {
        public override ComponentState GetComponentState()
        {
            return new ActionComponentState(CreateActionStatesArray());
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            /* TODO: Handle usages
            switch (message)
            {
                case ClickAlertMessage msg:
                {
                    var player = session.AttachedEntity;

                    if (player != Owner)
                    {
                        break;
                    }

                    if (!IsShowingAlert(msg.AlertType))
                    {
                        Logger.DebugS("alert", "user {0} attempted to" +
                                              " click alert {1} which is not currently showing for them",
                            player.Name, msg.AlertType);
                        break;
                    }

                    if (AlertManager.TryGet(msg.AlertType, out var alert))
                    {
                        alert.OnClick.AlertClicked(new ClickAlertEventArgs(player, alert));
                    }
                    else
                    {
                        Logger.WarningS("alert", "unrecognized encoded alert {0}", msg.AlertType);
                    }

                    break;
                }
            }*/
        }
    }

    public sealed class GrantAction : IClientCommand
    {
        public string Command => "grantaction";
        public string Description => "Grants an action to a player, defaulting to current player";
        public string Help => "grantaction <actionType> <name or userID, omit for current player>";
        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var attachedEntity = player.AttachedEntity;
            if (args.Length > 1)
            {
                var target = args[1];
                if (!Commands.CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (!CommandUtils.ValidateAttachedEntity(shell, player, attachedEntity)) return;


            if (!attachedEntity.TryGetComponent(out ServerActionsComponent actionsComponent))
            {
                shell.SendText(player, "user has no actions component");
                return;
            }

            var actionType = args[0];
            var actionMgr = IoCManager.Resolve<ActionManager>();
            if (!actionMgr.TryGet(Enum.Parse<ActionType>(actionType), out var action))
            {
                shell.SendText(player, "unrecognized actionType " + actionType);
                return;
            }
            actionsComponent.GrantAction(action.ActionType);
        }
    }

    public sealed class RevokeAction : IClientCommand
    {
        public string Command => "revokeaction";
        public string Description => "Revokes an action from a player, defaulting to current player";
        public string Help => "revokeaction <actionType> <name or userID, omit for current player>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var attachedEntity = player.AttachedEntity;
            if (args.Length > 1)
            {
                var target = args[1];
                if (!CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (!CommandUtils.ValidateAttachedEntity(shell, player, attachedEntity)) return;

            if (!attachedEntity.TryGetComponent(out ServerActionsComponent actionsComponent))
            {
                shell.SendText(player, "user has no actions component");
                return;
            }

            var actionType = args[0];
            var actionMgr = IoCManager.Resolve<ActionManager>();
            if (!actionMgr.TryGet(Enum.Parse<ActionType>(actionType), out var action))
            {
                shell.SendText(player, "unrecognized actionType " + actionType);
                return;
            }

            actionsComponent.RevokeAction(action.ActionType);
        }
    }
}
