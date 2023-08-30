using Content.Server.Ghost;
using Content.Server.Revenant.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class ShowGhostsCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "showghosts";
        public string Description => "makes all of the currently present ghosts visible. Cannot be reversed.";
        public string Help => "showghosts <visible>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!bool.TryParse(args[0], out var visible))
            {
                shell.WriteError(Loc.GetString("shell-invalid-bool"));
                return;
            }

            var ghostSys = _entities.EntitySysManager.GetEntitySystem<GhostSystem>();
            var revSys = _entities.EntitySysManager.GetEntitySystem<RevenantSystem>();

            ghostSys.MakeVisible(visible);
            revSys.MakeVisible(visible);
        }
    }
}
