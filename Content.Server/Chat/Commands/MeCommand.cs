using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal sealed class MeCommand : IConsoleCommand
    {
        private static readonly ProtoId<CommunicationChannelPrototype> ChatChannel = "Emote";

        public string Command => "me";
        public string Description => "Perform an action.";
        public string Help => "me <text>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteError("This command cannot be run from the server.");
                return;
            }

            if (player.Status != SessionStatus.InGame)
                return;

            if (player.AttachedEntity is not {} playerEntity)
            {
                shell.WriteError("You don't have an entity!");
                return;
            }

            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            IoCManager.Resolve<IChatManager>().SendChannelMessage(message, ChatChannel, shell.Player, playerEntity);
        }
    }
}
