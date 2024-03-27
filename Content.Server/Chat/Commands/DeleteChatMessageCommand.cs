using Content.Server.Administration;
using Content.Server.Chat.V2.Repository;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;

namespace Content.Server.Chat.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class DeleteChatMessageCommand : IConsoleCommand
{
    public string Command => "delmsg";
    public string Description => Loc.GetString("delete-message-command-description");
    public string Help => Loc.GetString("delete -message-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var repo = IoCManager.Resolve<ChatRepository>();

        if (!uint.TryParse(args[0], out var result))
        {
            shell.WriteError("can't delete chat message: invalid number argument");

            return;
        }

        repo.Delete(result);
    }
}
