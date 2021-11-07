using System;
using System.Collections.Generic;
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
        public string Help => $"{Command} <id> <x position> <y position> <gridId> [angle=<angle> zIndex=<zIndex> color=<color>]";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 4 || args.Length > 7)
            {
                shell.WriteError($"Received invalid amount of arguments arguments. Expected 4 to 7, got {args.Length}.\nUsage: {Help}");
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
            var zIndex = 0;
            Angle? rotation = null;
            if (args.Length > 4)
            {
                for (int i = 4; i < args.Length; i++)
                {
                    var rawValue = args[i].Split('=');
                    if (rawValue.Length != 2)
                    {
                        shell.WriteError($"Failed parsing parameter: {args[i]}");
                        return;
                    }

                    switch (rawValue[0])
                    {
                        case "angle":
                            if (!double.TryParse(rawValue[1], out var degrees))
                            {
                                shell.WriteError("Failed parsing angle.");
                                return;
                            }
                            rotation = Angle.FromDegrees(degrees);
                            break;
                        case "zIndex":
                            if (!int.TryParse(rawValue[1], out zIndex))
                            {
                                shell.WriteError("Failed parsing zIndex.");
                                return;
                            }
                            break;
                        case "color":
                            if (!Color.TryFromName(rawValue[1], out var colorRaw))
                            {
                                shell.WriteError("Failed parsing color.");
                                return;
                            }

                            color = colorRaw;
                            break;
                        default:
                            shell.WriteError($"Unknown named parameter key: {rawValue[0]}");
                            return;
                    }
                }
            }

            try
            {
                EntitySystem.Get<DecalSystem>().AddDecal(args[0], new GridId(gridIdRaw), new Vector2(x, y), color, rotation, zIndex);
            }
            catch (Exception e)
            {
                shell.WriteError($"Adding decal failed with the following message: {e.Message}");
            }
        }
    }
}
