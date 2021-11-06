using System;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Decals;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Server.Decals
{
    [AdminCommand(AdminFlags.Mapping)]
    public sealed class AddDecalCommand : IConsoleCommand
    {
        public string Command => "adddecal";
        public string Description => "Creates a decal on the map";
        public string Help => $"{Command} <id> <x position> <y position> <gridId> [<color>]";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 4)
            {
                shell.WriteError($"Received too little arguments. Expected at least 4, got {args.Length}.\nUsage: {Help}");
                return;
            }

            if (!float.TryParse(args[1], out var x))
            {
                shell.WriteError("Failed parsing x-coordinate.");
                return;
            }

            if (!float.TryParse(args[2], out var y))
            {
                shell.WriteError("Failed parsing y-coordinate");
                return;
            }

            if (!int.TryParse(args[3], out var gridIdRaw))
            {
                shell.WriteError("Failed parsing gridId");
                return;
            }

            Color? color = null;
            if (args.Length > 4)
            {
                if (Color.TryFromName(args[4], out var colorRaw))
                {
                    color = colorRaw;
                }
                else
                {
                    shell.WriteError("Failed parsing color.");
                    return;
                }
            }

            try
            {
                EntitySystem.Get<DecalSystem>().AddDecal(args[0], new GridId(gridIdRaw), new Vector2(x, y), color);
            }
            catch (Exception e)
            {
                shell.WriteError($"Adding decal failed with the following message: {e.Message}");
            }
        }
    }
}
