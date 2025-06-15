using Content.Server.Administration.Managers;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [UsedImplicitly]
    public sealed class PromoteHostCommand : IConsoleCommand
    {
        [Dependency] private readonly IAdminManager _adminMan = default!;
        [Dependency] private readonly IPlayerManager _playerMan = default!;

        public string Command => "promotehost";
        public string Description => "Grants client temporary full host admin privileges. Use this to bootstrap admins.";
        public string Help => "Usage promotehost <player>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine("Expected exactly one argument.");
                return;
            }

            if (!_playerMan.TryGetSessionByUsername(args[0], out var targetPlayer))
            {
                shell.WriteLine("Unable to find a player by that name.");
                return;
            }

            _adminMan.PromoteHost(targetPlayer);
        }
    }
}
