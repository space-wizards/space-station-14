using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Decals;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Decals.Commands
{
    [AdminCommand(AdminFlags.Mapping)]
    public sealed class AddDecalCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly TurfSystem _turfSystem = default!;
        [Dependency] private readonly MapSystem _mapSystem = default!;
        [Dependency] private readonly DecalSystem _decalSystem = default!;

        public override string Command => "adddecal";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 4 || args.Length > 7)
            {
                shell.WriteError(Loc.GetString("cmd-adddecal-received-invalid-amount-of-arguments", ("expected", 4), ("got", args.Length)));
                shell.WriteLine(Help);
                return;
            }

            if (!_protoManager.HasIndex<DecalPrototype>(args[0]))
            {
                shell.WriteError(Loc.GetString("cmd-adddecal-cannot-find-decalprototype", ("decalprototype", args[0])));
            }

            if (!float.TryParse(args[1], out var x))
            {
                shell.WriteError(Loc.GetString("cmd-adddecal-failed-parsing-x-coordinate", ("x-coordinate", args[1])));
                return;
            }

            if (!float.TryParse(args[2], out var y))
            {
                shell.WriteError(Loc.GetString("cmd-adddecal-failed-parsing-y-coordinate", ("y-coordinate", args[2])));
                return;
            }

            if (!NetEntity.TryParse(args[3], out var gridIdNet) ||
                !EntityManager.TryGetEntity(gridIdNet, out var gridIdRaw) ||
                !EntityManager.TryGetComponent(gridIdRaw, out MapGridComponent? grid))
            {
                shell.WriteError(Loc.GetString("cmd-adddecal-failed-parsing-gridId", ("gridId", args[3])));
                return;
            }

            var coordinates = new EntityCoordinates(gridIdRaw.Value, new Vector2(x, y));
            if (_turfSystem.IsSpace(_mapSystem.GetTileRef(gridIdRaw.Value, grid, coordinates)))
            {
                shell.WriteError(Loc.GetString("cmd-adddecal-cannot-create-decal-on-space-tile", ("coordinates", coordinates)));
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
                        shell.WriteError(Loc.GetString("cmd-adddecal-failed-parsing-parameter", ("parameter", args[i])));
                        return;
                    }

                    switch (rawValue[0])
                    {
                        case "angle":
                            if (!double.TryParse(rawValue[1], out var degrees))
                            {
                                shell.WriteError(Loc.GetString("cmd-adddecal-failed-parsing-angle", ("angle", rawValue[1])));
                                return;
                            }
                            rotation = Angle.FromDegrees(degrees);
                            break;
                        case "zIndex":
                            if (!int.TryParse(rawValue[1], out zIndex))
                            {
                                shell.WriteError(Loc.GetString("cmd-adddecal-failed-parsing-zIndex", ("zIndex", rawValue[1])));
                                return;
                            }
                            break;
                        case "color":
                            if (!Color.TryFromName(rawValue[1], out var colorRaw))
                            {
                                shell.WriteError(Loc.GetString("cmd-adddecal-failed-parsing-color", ("color", rawValue[1])));
                                return;
                            }

                            color = colorRaw;
                            break;
                        default:
                            shell.WriteError(Loc.GetString("cmd-adddecal-unknown-parameter-key", ("parameter-key", rawValue[0])));
                            return;
                    }
                }
            }

            if (_decalSystem.TryAddDecal(args[0], coordinates, out var uid, color, rotation, zIndex))
            {
                shell.WriteLine(Loc.GetString("cmd-adddecal-successfully-created-decal", ("decal", uid)));
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-adddecal-failed-adding-decal"));
            }
        }
    }
}
