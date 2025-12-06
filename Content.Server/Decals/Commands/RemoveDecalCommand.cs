using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server.Decals.Commands
{
    [AdminCommand(AdminFlags.Mapping)]
    public sealed class RemoveDecalCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly DecalSystem _decalSystem = default!;

        public override string Command => "rmdecal";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("cmd-rmdecal-error-args"));
                shell.WriteError(Help);
                return;
            }

            if (!uint.TryParse(args[0], out var uid))
            {
                shell.WriteError(Loc.GetString("cmd-rmdecal-error-uid"));
                return;
            }

            if (!NetEntity.TryParse(args[1], out var rawGridIdNet) ||
                !EntityManager.TryGetEntity(rawGridIdNet, out var rawGridId) ||
                !EntityManager.HasComponent<MapGridComponent>(rawGridId))
            {
                shell.WriteError(Loc.GetString("cmd-rmdecal-error-gridId"));
                return;
            }

            if (_decalSystem.RemoveDecal(rawGridId.Value, uid))
            {
                shell.WriteLine(Loc.GetString("cmd-rmdecal-success", ("uid", uid)));
                return;
            }

            shell.WriteError(Loc.GetString("cmd-rmdecal-error-remove", ("uid", uid)));
        }
    }
}
