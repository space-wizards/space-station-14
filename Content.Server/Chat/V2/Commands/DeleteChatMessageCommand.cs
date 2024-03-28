using Content.Server.Administration;
using Content.Server.Chat.V2.Repository;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;

namespace Content.Server.Chat.V2.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class DeleteChatMessageCommand : ToolshedCommand
{
    [CommandImplementation]
    public void DeleteChatMessage([PipedArgument] uint messageId)
    {
        IoCManager.Resolve<ChatRepository>().Delete(messageId);
    }
}
