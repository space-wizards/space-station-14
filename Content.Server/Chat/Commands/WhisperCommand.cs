using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal sealed class WhisperCommand : IConsoleCommand
    {
        private static readonly ProtoId<CommunicationChannelPrototype> ChatChannel = "ICWhisper";

        public string Command => "whisper";
        public string Description => "Send chat messages to the local channel as a whisper";
        public string Help => "whisper <text>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
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
