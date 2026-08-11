using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Decals;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server.Decals.Commands
{
    [AdminCommand(AdminFlags.Mapping)]
    public sealed partial class RemoveDecalCommand : IConsoleCommand
    {
        [Dependency] private IEntityManager _entManager = default!;

        public string Command => "rmdecal";
        public string Description => "removes a decal";
        public string Help => $"{Command} <chunkX> <chunkY> <uid> <gridId>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 4)
            {
                shell.WriteError($"Unexpected number of arguments.\nExpected four: {Help}");
                return;
            }

            if (!int.TryParse(args[0], out var chunkX) ||
                !int.TryParse(args[1], out var chunkY) ||
                !ushort.TryParse(args[2], out var uid))
            {
                shell.WriteError($"Failed parsing decal index.");
                return;
            }

            if (!NetEntity.TryParse(args[3], out var rawGridIdNet) ||
                !_entManager.TryGetEntity(rawGridIdNet, out var rawGridId) ||
                !_entManager.HasComponent<MapGridComponent>(rawGridId))
            {
                shell.WriteError("Failed parsing gridId.");
                return;
            }

            var decalSystem = _entManager.System<DecalSystem>();
            var index = new DecalIndex(new Vector2i(chunkX, chunkY), uid);
            if (decalSystem.RemoveDecal(rawGridId.Value, index))
            {
                shell.WriteLine($"Successfully removed decal {index}.");
                return;
            }

            shell.WriteError($"Failed trying to remove decal {index}.");
        }
    }
}
