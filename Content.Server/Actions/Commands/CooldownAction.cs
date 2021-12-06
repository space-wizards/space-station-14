using System;
using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Actions;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server.Actions.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class CooldownAction : IConsoleCommand
    {
        public string Command => "coolaction";
        public string Description => "Sets a cooldown on an action for a player, defaulting to current player";
        public string Help => "coolaction <actionType> <seconds> <name or userID, omit for current player>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player?.AttachedEntity is not {} attachedEntity) return;
            if (args.Length > 2)
            {
                var target = args[2];
                if (!CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(attachedEntity, out ServerActionsComponent? actionsComponent))
            {
                shell.WriteError("user has no actions component");
                return;
            }

            var actionTypeRaw = args[0];
            if (!Enum.TryParse<ActionType>(actionTypeRaw, out var actionType))
            {
                shell.WriteLine("unrecognized ActionType enum value, please" +
                                " ensure you used correct casing: " + actionTypeRaw);
                return;
            }
            var actionMgr = IoCManager.Resolve<ActionManager>();

            if (!actionMgr.TryGet(actionType, out var action))
            {
                shell.WriteLine("unrecognized actionType " + actionType);
                return;
            }

            var cooldownStart = IoCManager.Resolve<IGameTiming>().CurTime;
            if (!uint.TryParse(args[1], out var seconds))
            {
                shell.WriteLine("cannot parse seconds: " + args[1]);
                return;
            }

            var cooldownEnd = cooldownStart.Add(TimeSpan.FromSeconds(seconds));

            actionsComponent.Cooldown(action.ActionType, (cooldownStart, cooldownEnd));
        }
    }
}
