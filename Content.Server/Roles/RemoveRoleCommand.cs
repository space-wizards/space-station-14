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
    public sealed class RemoveRoleCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public string Command => "rmrole";

        public string Description => "Removes a role from a player's mind.";

        public string Help => "rmrole <session ID> <Role Type>\nThat role type is the actual C# type name.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine("Expected exactly 2 arguments.");
                return;
            }

            var mgr = IoCManager.Resolve<IPlayerManager>();
            if (!mgr.TryGetPlayerDataByUsername(args[0], out var data))
            {
                shell.WriteLine("Can't find that mind");
                return;
            }

            var mind = data.ContentData()?.Mind;

            if (mind == null)
            {
                shell.WriteLine("Can't find that mind");
                return;
            }

            var roles = _entityManager.System<SharedRoleSystem>();
            var jobs = _entityManager.System<SharedJobSystem>();
            if (jobs.MindHasJobWithId(mind, args[1]))
                roles.MindRemoveRole<JobRoleComponent>(mind.Value);
        }
    }
}
