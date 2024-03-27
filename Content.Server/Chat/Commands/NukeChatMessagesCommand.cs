using Content.Server.Administration;
using Content.Server.Chat.V2.Repository;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;

namespace Content.Server.Chat.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class NukeChatMessagesForUsernamesCommand : ToolshedCommand
{
    [Dependency] private readonly ChatRepository _repository = default!;

    public void NukeChatMessagesForUsernames([PipedArgument] IEnumerable<string> usernames)
    {
        foreach (var username in usernames)
        {
            _repository.NukeForUsername(username, out _);
        }
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class NukeChatMessagesForUserIDsCommand : ToolshedCommand
{
    [Dependency] private readonly ChatRepository _repository = default!;
    public void NukeChatMessagesForUserIDs([PipedArgument] IEnumerable<string> userIDs)
    {
        foreach (var username in userIDs)
        {
            _repository.NukeForUserId(username, out _);
        }
    }
}
