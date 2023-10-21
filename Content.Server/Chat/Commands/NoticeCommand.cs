using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.Chat.Commands;

[AnyCommand]
internal sealed class NoticeCommand : IConsoleCommand
{
    [Dependency] private readonly IChatManager _chatManager = default!;

    public string Command => "notice";
    public string Description => Loc.GetString("notice-command-description");
    public string Help => Loc.GetString("notice-command-help", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not IPlayerSession player)
        {
            shell.WriteError("This command cannot be run from the server.");
            return;
        }

        if (player.Status != SessionStatus.InGame)
            return;

        if (player.AttachedEntity is not { })
        {
            shell.WriteError("You don't have an entity!");
            return;
        }

        if (args.Length < 1)
            return;

        var message = string.Join(" ", args).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        _chatManager.ChatMessageToOne(ChatChannel.Visual, message, message, EntityUid.Invalid, false, player.ConnectedClient, recordReplay: false);
    }
}
