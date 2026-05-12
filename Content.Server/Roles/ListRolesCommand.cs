using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Roles
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class ListRolesCommand : LocalizedCommands
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Command => "listroles";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 0)
            {
                shell.WriteLine(Loc.GetString($"shell-need-exactly-zero-arguments"));
                return;
            }

            foreach(var job in _prototypeManager.EnumeratePrototypes<JobPrototype>())
            {
                shell.WriteLine(job.ID);
            }
        }
    }
}
