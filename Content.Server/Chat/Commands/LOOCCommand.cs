using Content.Server.Chat.Systems;
using Content.Server.Commands;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands;

[AnyCommand]
internal sealed class LoocCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    public override string Command => "looc";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!CommandChecks.MustBeAttachedToEntity(shell, out var player, out var entity) ||
            !CommandChecks.NeedExactlyOneArgument(shell, args))
            return;

        var message = string.Join(" ", args).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        _chatSystem.TrySendInGameOOCMessage(entity.Value, message, InGameOOCChatType.Looc, false, shell, player);
    }
}
