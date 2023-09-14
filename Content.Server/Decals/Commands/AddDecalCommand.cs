using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Decals;
using Content.Shared.Maps;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Decals.Commands
{
    [AdminCommand(AdminFlags.Mapping)]
    public sealed class AddDecalCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;

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

            if (!_protoManager.HasIndex<DecalPrototype>(args[0]))
            {
                shell.WriteError($"Cannot find decalprototype '{args[0]}'.");
            }

            if (!float.TryParse(args[1], out var x))
            {
                shell.WriteError($"Failed parsing x-coordinate '{args[1]}'.");
                return;
            }

            if (!float.TryParse(args[2], out var y))
            {
                shell.WriteError($"Failed parsing y-coordinate'{args[2]}'.");
                return;
            }

            if (!NetEntity.TryParse(args[3], out var gridIdNet) ||
                !_entManager.TryGetEntity(gridIdNet, out var gridIdRaw) ||
                !_mapManager.TryGetGrid(gridIdRaw, out var grid))
            {
                shell.WriteError($"Failed parsing gridId '{args[3]}'.");
                return;
            }

            var coordinates = new EntityCoordinates(grid.Owner, new Vector2(x, y));
            if (grid.GetTileRef(coordinates).IsSpace())
            {
                shell.WriteError($"Cannot create decal on space tile at {coordinates}.");
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
                        shell.WriteError($"Failed parsing parameter: '{args[i]}'");
                        return;
                    }

                    switch (rawValue[0])
                    {
                        case "angle":
                            if (!double.TryParse(rawValue[1], out var degrees))
                            {
                                shell.WriteError($"Failed parsing angle '{rawValue[1]}'.");
                                return;
                            }
                            rotation = Angle.FromDegrees(degrees);
                            break;
                        case "zIndex":
                            if (!int.TryParse(rawValue[1], out zIndex))
                            {
                                shell.WriteError($"Failed parsing zIndex '{rawValue[1]}'.");
                                return;
                            }
                            break;
                        case "color":
                            if (!Color.TryFromName(rawValue[1], out var colorRaw))
                            {
                                shell.WriteError($"Failed parsing color '{rawValue[1]}'.");
                                return;
                            }

                            color = colorRaw;
                            break;
                        default:
                            shell.WriteError($"Unknown parameter key '{rawValue[0]}'.");
                            return;
                    }
                }
            }

            if (_entManager.System<DecalSystem>().TryAddDecal(args[0], coordinates, out var uid, color, rotation, zIndex))
            {
                shell.WriteLine($"Successfully created decal {uid}.");
            }
            else
            {
                shell.WriteError($"Failed adding decal.");
            }
        }
    }
}
