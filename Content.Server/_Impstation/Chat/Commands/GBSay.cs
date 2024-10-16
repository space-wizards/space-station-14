using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Content.Shared._Impstation.Ghost;
using Robust.Shared.Console;

namespace Content.Server._Impstation.Commands
{
    [AnyCommand]
    sealed class GBSay : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _e = default!;

        public string Command => "gbsay"; // Ghost bar say???? whatever man. it just needs a name

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

            if (!_e.TryGetComponent(player.AttachedEntity, out GhostBarPatronComponent? ghostBarComp))
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
