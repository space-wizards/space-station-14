using Content.Server.Administration;
using Content.Server.Chat.V2.Repository;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;

namespace Content.Server.Chat.V2.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class NukeChatMessagesForUsernamesCommand : ToolshedCommand
{
    public void NukeChatMessagesForUsernames([PipedArgument] IEnumerable<string> usernames)
    {
        foreach (var username in usernames)
        {
            IoCManager.Resolve<ChatRepository>().NukeForUsername(username, out _);
        }
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class NukeChatMessagesForUserIDsCommand : ToolshedCommand
{
    public void NukeChatMessagesForUserIDs([PipedArgument] IEnumerable<string> userIDs)
    {
        foreach (var username in userIDs)
        {
            IoCManager.Resolve<ChatRepository>().NukeForUserId(username, out _);
        }
    }
}
