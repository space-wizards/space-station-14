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
        public string Help => "showghosts";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var ghostSys = _entities.EntitySysManager.GetEntitySystem<GhostSystem>();
            var revSys = _entities.EntitySysManager.GetEntitySystem<RevenantSystem>();

            ghostSys.MakeVisible();
            revSys.MakeVisible();
        }
    }
}
