using Content.Server.Chat.Managers;
using Content.Server.Commands;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands;

[AnyCommand]
internal sealed class OOCCommand : LocalizedCommands
{
    [Dependency] private readonly IChatManager _chatManager = default!;

    public override string Command => "ooc";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!CommandChecks.MustNotBeServer(shell, out var player) ||
            !CommandChecks.NeedExactlyOneArgument(shell, args))
            return;

        var message = string.Join(" ", args).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        _chatManager.TrySendOOCMessage(player, message, OOCChatType.OOC);
    }
}
