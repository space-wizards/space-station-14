using Content.Server.Chat.Managers;
using Content.Server.DeadSpace.Prison;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal sealed class OOCCommand : LocalizedCommands
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override string Command => "ooc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteError(Loc.GetString($"shell-cannot-run-command-from-server"));
                return;
            }

            if (args.Length < 1)
                return;

            if (_entityManager.System<PrisonSystem>().IsUserPrisoner(player.UserId))
            {
                _chatManager.DispatchServerMessage(player, Loc.GetString("prison-ooc-blocked"));
                return;
            }

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            _chatManager.TrySendOOCMessage(player, message, OOCChatType.OOC);
        }
    }
}
