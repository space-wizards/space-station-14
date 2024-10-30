using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Moderator)]
    sealed class DSay : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _e = default!;

        public string Command => "dsay";

        public string Description => Loc.GetString("dsay-command-description");

        public string Help => Loc.GetString("dsay-command-help-text", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            if (player.AttachedEntity is not { Valid: true } entity)
                return;

            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            var chat = _e.System<ChatSystem>();
            chat.TrySendInGameOOCMessage(entity, message, InGameOOCChatType.Dead, false, shell, player);
        }
    }
}
