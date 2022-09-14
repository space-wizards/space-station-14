using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Afk
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class IsAfkCommand : IConsoleCommand
    {
        [Dependency] private readonly IPlayerManager _players = default!;

        public string Command => "isafk";
        public string Description => "Checks if a specified player is AFK";
        public string Help => "Usage: isafk <playerName>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var afkManager = IoCManager.Resolve<IAfkManager>();

            if (args.Length == 0)
            {
                shell.WriteError("Need one argument");
                return;
            }

            if (!_players.TryGetSessionByUsername(args[0], out var player))
            {
                shell.WriteError("Unable to find that player");
                return;
            }

            shell.WriteLine(afkManager.IsAfk(player) ? "They are indeed AFK" : "They are not AFK");
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                return CompletionResult.FromHintOptions(
                    CompletionHelper.SessionNames(players: _players),
                    "<playerName>");
            }

            return CompletionResult.Empty;
        }
    }
}
