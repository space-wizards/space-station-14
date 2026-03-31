using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Server.Commands;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands;

[AdminCommand(AdminFlags.Adminchat)]
public sealed class AdminChatCommand : LocalizedCommands
{
    [Dependency] private readonly IChatManager _chatManager = default!;

    public override string Command => "asay";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!CommandChecks.MustNotBeServer(shell, out var player) ||
            !CommandChecks.NeedExactlyOneArgument(shell, args))
            return;

        var message = string.Join(" ", args).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        _chatManager.TrySendOOCMessage(player, message, OOCChatType.Admin);
    }
}
