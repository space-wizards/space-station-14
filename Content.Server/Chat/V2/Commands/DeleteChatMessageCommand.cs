using Content.Server.Administration;
using Content.Server.Chat.V2.Repository;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server.Chat.V2.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class DeleteChatMessageCommand : ToolshedCommand
{
    public string Description => Loc.GetString("delete-message-command-description");
    public string Help => Loc.GetString("delete -message-command-help");

    [CommandImplementation]
    public void DeleteChatMessage([PipedArgument] uint messageId)
    {
        IoCManager.Resolve<ChatRepository>().Delete(messageId);
    }
}
