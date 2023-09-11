using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using SQLitePCL;

namespace Content.Server.Decals.Commands
{
    [AdminCommand(AdminFlags.Mapping)]
    public sealed class RemoveDecalCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        public string Command => "rmdecal";
        public string Description => "removes a decal";
        public string Help => $"{Command} <uid> <gridId>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteError($"Unexpected number of arguments.\nExpected two: {Help}");
                return;
            }

            if (!uint.TryParse(args[0], out var uid))
            {
                shell.WriteError($"Failed parsing uid.");
                return;
            }

            if (!NetEntity.TryParse(args[1], out var rawGridIdNet) ||
                !_entManager.TryGetEntity(rawGridIdNet, out var rawGridId) ||
                !_mapManager.GridExists(rawGridId))
            {
                shell.WriteError("Failed parsing gridId.");
                return;
            }

            var decalSystem = _entManager.System<DecalSystem>();
            if (decalSystem.RemoveDecal(rawGridId.Value, uid))
            {
                shell.WriteLine($"Successfully removed decal {uid}.");
                return;
            }

            shell.WriteError($"Failed trying to remove decal {uid}.");
        }
    }
}
