using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Moderator)]
    sealed class DSay : IConsoleCommand
    {
        private static readonly ProtoId<CommunicationChannelPrototype> ChatChannel = "Dead";

        [Dependency] private readonly IEntityManager _e = default!;
        [Dependency] private readonly IChatManager _chat = default!;

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

            _chat.SendChannelMessage(message, ChatChannel, player, entity);
        }
    }
}
