using Content.Server.Administration;
using Content.Server.Chat.V2.Repository;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;

namespace Content.Server.Chat.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class DeleteChatMessageCommand : ToolshedCommand
{
    [Dependency] private readonly ChatRepository _repository = default!;

    [CommandImplementation]
    public void DeleteChatMessage([PipedArgument] uint messageId)
    {
        _repository.Delete(messageId);
    }
}
