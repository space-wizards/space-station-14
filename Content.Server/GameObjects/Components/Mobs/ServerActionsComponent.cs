using System;
using Content.Server.Actions;
using Content.Server.Commands;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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

            switch (message)
            {
                case PerformInstantActionMessage msg:
                {
                    var player = session.AttachedEntity;

                    if (player != Owner)
                    {
                        break;
                    }

                    if (!IsGranted(msg.ActionType))
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform instant action {1} which is not granted to them", player.Name,
                            msg.ActionType);
                        break;
                    }

                    if (!ActionManager.TryGet(msg.ActionType, out var actionShared))
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform unrecognized instant action {1}", player.Name,
                            msg.ActionType);
                        break;
                    }

                    var action = actionShared as ActionPrototype;


                    if (action.InstantAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as an instant action, but it isn't one", player.Name,
                            msg.ActionType);
                        break;
                    }

                    action.InstantAction.DoInstantAction(new InstantActionEventArgs(player));

                    break;
                }
            }
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
