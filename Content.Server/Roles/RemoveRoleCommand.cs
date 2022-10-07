using Content.Server.Administration;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Roles
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class RemoveRoleCommand : IConsoleCommand
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

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

            var role = new Job(mind, _prototypeManager.Index<JobPrototype>(args[1]));
            mind.RemoveRole(role);
        }
    }
}
