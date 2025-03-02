using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.Chat.Commands;

[AnyCommand]
internal sealed class ChatCommand : IConsoleCommand
{
    public string Command => "chat";
    public string Description => "Send chat messages via any chat communication type.";
    public string Help => "chat <communicationType> <text>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (player.Status != SessionStatus.InGame)
            return;

        if (args.Length < 2)
            return;

        var communicationChannel = args[0];

        var message = args[1];
        if (string.IsNullOrEmpty(message))
            return;

        // CHAT-TODO: Set the senderEntity here
        IoCManager.Resolve<IChatManager>().SendChannelMessage(message, communicationChannel, shell.Player, player.AttachedEntity);
    }
}
