using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal sealed class TryCommand : IConsoleCommand
    {
        public string Command => "try";
        public string Description => "Perform an emotion with a chance.";
        public string Help => "try <text>";

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

            var tryRandom = new Random().Next(0, 2) == 0; 
            var trueFalse = tryRandom ? $"[color=#3d8c40](Success!)[/color]" : "[color=#a91409](Unsuccess...)[/color]";
            var fullMessage = $"{message} {trueFalse}";

            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>()
                .TrySendInGameICMessage(playerEntity, fullMessage, InGameICChatType.Emote, ChatTransmitRange.Normal, false, shell, player);
        }
    }
}
