using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Players;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Roles
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class RemoveRoleCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly SharedJobSystem _jobs = default!;
        [Dependency] private readonly SharedRoleSystem _roles = default!;

        public override string Command => "rmrole";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine(Loc.GetString($"shell-wrong-arguments-number-need-specific",
                    ("properAmount", 2),
                    ("currentAmount", args.Length)));
                return;
            }

            if (!_playerManager.TryGetPlayerDataByUsername(args[0], out var data))
            {
                shell.WriteLine(Loc.GetString($"cmd-addrole-mind-not-found"));
                return;
            }

            var mind = data.ContentData()?.Mind;

            if (mind == null)
            {
                shell.WriteLine(Loc.GetString($"cmd-addrole-mind-not-found"));
                return;
            }

            if (_jobs.MindHasJobWithId(mind, args[1]))
                _roles.MindRemoveRole<JobRoleComponent>(mind.Value);
        }
    }
}
