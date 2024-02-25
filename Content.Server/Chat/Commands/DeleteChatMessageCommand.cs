using Content.Server.Administration;
using Content.Server.Chat.V2.Repository;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    internal sealed class DeleteChatMessageCommand : IConsoleCommand
    {
        public string Command => "delmsg";
        public string Description => "Delete a specific chat message";
        public string Help => "delmsg <id>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<IEntityManager>().System<ChatRepository>().Delete(uint.Parse(args[0]));
        }
    }
}

