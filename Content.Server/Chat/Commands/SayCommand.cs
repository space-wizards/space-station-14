using Content.Server.Chat.Systems;
using Content.Server.Commands;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands;

[AnyCommand]
public sealed class SayCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    public override string Command => "say";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!CommandChecks.MustBeAttachedToEntity(shell, out var player, out var entity) ||
            !CommandChecks.NeedExactlyOneArgument(shell, args))
            return;

        var message = string.Join(" ", args).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        _chatSystem.TrySendInGameICMessage(entity.Value, message, InGameICChatType.Speak, ChatTransmitRange.Normal, false, shell, player);
    }
}
