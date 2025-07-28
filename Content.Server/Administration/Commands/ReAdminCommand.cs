using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AnyCommand]
    public sealed class ReAdminCommand : LocalizedCommands
    {
        [Dependency] private readonly IAdminManager _adminManager = default!;

        public override string Command => "readmin";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine(Loc.GetString($"shell-cannot-run-command-from-server"));
                return;
            }

            if (_adminManager.GetAdminData(player, includeDeAdmin: true) == null)
            {
                shell.WriteLine(Loc.GetString($"cmd-readmin-not-an-admin"));
                return;
            }

            _adminManager.ReAdmin(player);
        }
    }
}
