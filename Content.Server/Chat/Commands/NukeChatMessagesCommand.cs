using Content.Server.Administration;
using Content.Server.Chat.V2.Repository;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    internal sealed class NukeChatMessagesCommand : IConsoleCommand
    {
        public string Command => "nuke";
        public string Description => "Delete all of a user's specific chat message";
        public string Help => "nuke <username>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<IEntityManager>().System<ChatRepository>().Nuke(args[0]);
        }
    }
}

