using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Decals.Commands
{
    [AdminCommand(AdminFlags.Mapping)]
    public class RemoveDecalCommand : IConsoleCommand
    {
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

            if (!int.TryParse(args[1], out var rawGridId) ||
                !IoCManager.Resolve<IMapManager>().GridExists(new GridId(rawGridId)))
            {
                shell.WriteError("Failed parsing gridId.");
            }

            var decalSystem = EntitySystem.Get<DecalSystem>();
            if (decalSystem.RemoveDecal(new GridId(rawGridId), uid))
            {
                shell.WriteLine($"Successfully removed decal {uid}.");
                return;
            }

            shell.WriteError($"Failed trying to remove decal {uid}.");
        }
    }
}
