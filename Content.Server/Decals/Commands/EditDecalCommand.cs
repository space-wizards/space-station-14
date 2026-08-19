using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Decals;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server.Decals;

[AdminCommand(AdminFlags.Mapping)]
public sealed partial class EditDecalCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entManager = default!;

    public string Command => "editdecal";
    public string Description => "Edits a decal.";
    public string Help => $@"{Command} <gridId> <chunkX> <chunkY> <uid> <mode>\n
Possible modes are:\n
- position <x position> <y position>\n
- color <color>\n
- id <id>\n
- rotation <degrees>\n
- zindex <zIndex>\n
- clean <cleanable>
";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 6)
        {
            shell.WriteError("Expected at least 6 arguments.");
            return;
        }

        if (!NetEntity.TryParse(args[0], out var gridIdNet) || !_entManager.TryGetEntity(gridIdNet, out var gridId))
        {
            shell.WriteError($"Failed parsing gridId '{args[3]}'.");
            return;
        }

        if (!int.TryParse(args[1], out var chunkX) ||
            !int.TryParse(args[2], out var chunkY) ||
            !ushort.TryParse(args[3], out var uid))
        {
            shell.WriteError("Failed parsing decal index.");
            return;
        }

        var index = new DecalIndex(new Vector2i(chunkX, chunkY), uid);

        if (!_entManager.HasComponent<MapGridComponent>(gridId))
        {
            shell.WriteError($"No grid with gridId {gridId} exists.");
            return;
        }

        var decalSystem = _entManager.System<DecalSystem>();
        switch (args[4].ToLower())
        {
            case "position":
                if(args.Length != 7)
                {
                    shell.WriteError("Expected 7 arguments.");
                    return;
                }

                if (!float.TryParse(args[5], out var x) || !float.TryParse(args[6], out var y))
                {
                    shell.WriteError("Failed parsing position.");
                    return;
                }

                if (!decalSystem.SetDecalPosition(gridId.Value, index, new(gridId.Value, new Vector2(x, y))))
                {
                    shell.WriteError("Failed changing decalposition.");
                }
                break;
            case "color":
                if(args.Length != 6)
                {
                    shell.WriteError("Expected 6 arguments.");
                    return;
                }

                if (!Color.TryFromName(args[5], out var color))
                {
                    shell.WriteError("Failed parsing color.");
                    return;
                }

                if (!decalSystem.SetDecalColor(gridId.Value, index, color))
                {
                    shell.WriteError("Failed changing decal color.");
                }
                break;
            case "id":
                if(args.Length != 6)
                {
                    shell.WriteError("Expected 6 arguments.");
                    return;
                }

                if (!decalSystem.SetDecalId(gridId.Value, index, args[5]))
                {
                    shell.WriteError("Failed changing decal id.");
                }
                break;
            case "rotation":
                if(args.Length != 6)
                {
                    shell.WriteError("Expected 6 arguments.");
                    return;
                }

                if (!double.TryParse(args[5], out var degrees))
                {
                    shell.WriteError("Failed parsing degrees.");
                    return;
                }

                if (!decalSystem.SetDecalRotation(gridId.Value, index, Angle.FromDegrees(degrees)))
                {
                    shell.WriteError("Failed changing decal rotation.");
                }
                break;
            case "zindex":
                if(args.Length != 6)
                {
                    shell.WriteError("Expected 6 arguments.");
                    return;
                }

                if (!int.TryParse(args[5], out var zIndex))
                {
                    shell.WriteError("Failed parsing zIndex.");
                    return;
                }

                if (!decalSystem.SetDecalZIndex(gridId.Value, index, zIndex))
                {
                    shell.WriteError("Failed changing decal zIndex.");
                }
                break;
            case "clean":
                if(args.Length != 6)
                {
                    shell.WriteError("Expected 6 arguments.");
                    return;
                }

                if (!bool.TryParse(args[5], out var cleanable))
                {
                    shell.WriteError("Failed parsing cleanable.");
                    return;
                }

                if (!decalSystem.SetDecalCleanable(gridId.Value, index, cleanable))
                {
                    shell.WriteError("Failed changing decal cleanable flag.");
                }
                break;
            default:
                shell.WriteError("Invalid mode.");
                return;
        }
    }
}
