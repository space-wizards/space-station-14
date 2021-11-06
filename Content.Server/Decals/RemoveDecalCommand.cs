using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Server.Decals
{
    [AdminCommand(AdminFlags.Mapping)]
    public class RemoveDecalCommand : IConsoleCommand
    {
        public string Command => "rmdecal";
        public string Description => "removes a decal";
        public string Help => $"{Command} <uid>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError($"Unexpected number of arguments.\nExpected one: {Help}");
                return;
            }

            if (!uint.TryParse(args[0], out var uid))
            {
                shell.WriteError($"Failed parsing uid.");
                return;
            }

            var decalSystem = EntitySystem.Get<DecalSystem>();
            if (decalSystem.RemoveDecal(uid))
            {
                shell.WriteLine($"Successfully removed decal {uid}.");
                return;
            }

            shell.WriteError($"Failed trying to remove decal {uid}.");
        }
    }
}
