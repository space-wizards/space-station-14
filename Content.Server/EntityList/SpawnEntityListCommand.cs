using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.EntityList;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityList
{
    [AdminCommand(AdminFlags.Spawn)]
    public sealed class SpawnEntityListCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Command => "spawnentitylist";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString($"shell-need-exactly-one-argument"));
                return;
            }

            if (shell.Player is not { } player)
            {
                shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            if (player.AttachedEntity is not {} attached)
            {
                shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
                return;
            }

            if (!_prototypeManager.TryIndex(args[0], out EntityListPrototype? prototype))
            {
                shell.WriteError(Loc.GetString($"cmd-spawnentitylist-failed",
                    ("prototype", nameof(EntityListPrototype)),
                    ("id", args[0])));
                return;
            }

            var i = 0;

            foreach (var entity in prototype.Entities(_prototypeManager))
            {
                EntityManager.SpawnEntity(entity.ID, EntityManager.GetComponent<TransformComponent>(attached).Coordinates);
                i++;
            }

            shell.WriteLine(Loc.GetString($"cmd-spawnentitylist-success", ("count", i)));
        }
    }
}
