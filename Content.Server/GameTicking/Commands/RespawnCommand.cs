using Content.Server.Administration;
using Content.Server.Mind;
using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server.GameTicking.Commands
{
    sealed class RespawnCommand : LocalizedCommands
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IPlayerLocator _locator = default!;
        [Dependency] private readonly IEntitySystemManager _systems = default!;

        public override string Command => "respawn";

        public async override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (!_systems.TryGetEntitySystem<GameTicker>(out var gameTicker) || !_systems.TryGetEntitySystem<MindSystem>(out var mind))
                return;

            var player = shell.Player;
            if (args.Length > 1)
            {
                shell.WriteLine(Loc.GetString("cmd-respawn-invalid-args"));
                return;
            }

            NetUserId userId;
            if (args.Length == 0)
            {
                if (player == null)
                {
                    shell.WriteLine(Loc.GetString("cmd-respawn-no-player"));
                    return;
                }

                userId = player.UserId;
            }
            else
            {
                var located = await _locator.LookupIdByNameOrIdAsync(args[0]);

                if (located == null)
                {
                    shell.WriteLine(Loc.GetString("cmd-respawn-unknown-player"));
                    return;
                }

                userId = located.UserId;
            }

            if (!_player.TryGetSessionById(userId, out var targetPlayer))
            {
                if (!_player.TryGetPlayerData(userId, out var data))
                {
                    shell.WriteLine(Loc.GetString("cmd-respawn-unknown-player"));
                    return;
                }

                mind.WipeMind(data.ContentData()?.Mind);
                shell.WriteLine(Loc.GetString("cmd-respawn-player-not-online"));
                return;
            }

            gameTicker.Respawn(targetPlayer);
        }
    }
}
