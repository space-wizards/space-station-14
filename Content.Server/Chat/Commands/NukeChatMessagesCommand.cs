using Content.Server.Administration;
using Content.Server.Chat.V2.Repository;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;

namespace Content.Server.Chat.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class NukeChatMessagesUsernameCommand : IConsoleCommand
{
    public string Command => "nukeusernames";
    public string Description => Loc.GetString("nuke-messages-username-command-description");
    public string Help => Loc.GetString("nuke-messages-username-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var repo = IoCManager.Resolve<ChatRepository>();

        if (args.Length <= 0)
        {
            shell.WriteError($"nuking messages failed: you forgot to input a username!");

            return;
        }

        foreach (var username in args)
        {
            if (!repo.NukeForUsername(username, out var reason))
            {
                shell.WriteError($"nuke for username {args[0]} failed: {reason}");
            }
        }
    }

}

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class NukeChatMessagesUserIdCommand : IConsoleCommand
{
    public string Command => "nukeuserids";
    public string Description => Loc.GetString("nuke-messages-id-command-description");
    public string Help => Loc.GetString("nuke-messages-id-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var repo = IoCManager.Resolve<ChatRepository>();

        if (args.Length <= 0)
        {
            shell.WriteError($"nuking messages failed: you forgot to input a userId!");

            return;
        }

        foreach (var username in args)
        {
            if (!repo.NukeForUserId(username, out var reason))
            {
                shell.WriteError($"nuke for userId {args[0]} failed: {reason}");
            }
        }
    }
}
