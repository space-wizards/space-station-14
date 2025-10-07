using System.Linq;
using Content.Server.Administration;
using Content.Server.Mind;
using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server.GameTicking.Commands
{
    sealed class RespawnCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IPlayerLocator _locator = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly MindSystem _mind = default!;

        public override string Command => "respawn";

        public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

        public override async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (args.Length > 1)
            {
                shell.WriteError(Loc.GetString($"cmd-{Command}-invalid-args"));
                return;
            }

            NetUserId userId;
            if (args.Length == 0)
            {
                if (player == null)
                {
                    shell.WriteError(Loc.GetString($"cmd-{Command}-no-player"));
                    return;
                }

                userId = player.UserId;
            }
            else
            {
                var located = await _locator.LookupIdByNameOrIdAsync(args[0]);

                if (located == null)
                {
                    shell.WriteError(Loc.GetString($"cmd-{Command}-unknown-player"));
                    return;
                }

                userId = located.UserId;
            }

            if (!_player.TryGetSessionById(userId, out var targetPlayer))
            {
                if (!_player.TryGetPlayerData(userId, out var data))
                {
                    shell.WriteError(Loc.GetString($"cmd-{Command}-unknown-player"));
                    return;
                }

                _mind.WipeMind(data.ContentData()?.Mind);
                shell.WriteError(Loc.GetString($"cmd-{Command}-player-not-online"));
                return;
            }

            _gameTicker.Respawn(targetPlayer);
        }

      public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length != 1)
                return CompletionResult.Empty;

            var options = _player.Sessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();

            return CompletionResult.FromHintOptions(options, Loc.GetString($"cmd-{Command}-player-completion"));
        }
    }
}
