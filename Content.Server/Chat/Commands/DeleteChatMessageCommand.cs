using Content.Server.Administration;
using Content.Server.Chat.V2.Repository;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    internal sealed class DeleteChatMessageCommand : IConsoleCommand
    {
        [Dependency] private readonly ChatRepository _repo = default!;

        public string Command => "delmsg";
        public string Description => "Delete a specific chat message";
        public string Help => "delmsg <id>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (!uint.TryParse(args[0], out var result))
            {
                shell.WriteError("can't delete chat message: invalid number argument");

                return;
            }

            _repo.Delete(result);
        }
    }
}

