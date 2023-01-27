using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Decals;

[AdminCommand(AdminFlags.Mapping)]
public sealed class EditDecalCommand : IConsoleCommand
{
    public string Command => "editdecal";
    public string Description => "Edits a decal.";
    public string Help => $@"{Command} <gridId> <uid> <mode>\n
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
        if (args.Length < 4)
        {
            shell.WriteError("Expected at least 5 arguments.");
            return;
        }

        if (!EntityUid.TryParse(args[0], out var gridId))
        {
            shell.WriteError($"Failed parsing gridId '{args[3]}'.");
            return;
        }

        if (!uint.TryParse(args[1], out var uid))
        {
            shell.WriteError($"Failed parsing uid '{args[1]}'.");
            return;
        }

        if (!IoCManager.Resolve<IMapManager>().GridExists(gridId))
        {
            shell.WriteError($"No grid with gridId {gridId} exists.");
            return;
        }

        var decalSystem = EntitySystem.Get<DecalSystem>();
        switch (args[2].ToLower())
        {
            case "position":
                if(args.Length != 5)
                {
                    shell.WriteError("Expected 6 arguments.");
                    return;
                }

                if (!float.TryParse(args[3], out var x) || !float.TryParse(args[4], out var y))
                {
                    shell.WriteError("Failed parsing position.");
                    return;
                }

                if (!decalSystem.SetDecalPosition(gridId, uid, new(gridId, (x, y))))
                {
                    shell.WriteError("Failed changing decalposition.");
                }
                break;
            case "color":
                if(args.Length != 4)
                {
                    shell.WriteError("Expected 5 arguments.");
                    return;
                }

                if (!Color.TryFromName(args[3], out var color))
                {
                    shell.WriteError("Failed parsing color.");
                    return;
                }

                if (!decalSystem.SetDecalColor(gridId, uid, color))
                {
                    shell.WriteError("Failed changing decal color.");
                }
                break;
            case "id":
                if(args.Length != 4)
                {
                    shell.WriteError("Expected 5 arguments.");
                    return;
                }

                if (!decalSystem.SetDecalId(gridId, uid, args[3]))
                {
                    shell.WriteError("Failed changing decal id.");
                }
                break;
            case "rotation":
                if(args.Length != 4)
                {
                    shell.WriteError("Expected 5 arguments.");
                    return;
                }

                if (!double.TryParse(args[3], out var degrees))
                {
                    shell.WriteError("Failed parsing degrees.");
                    return;
                }

                if (!decalSystem.SetDecalRotation(gridId, uid, Angle.FromDegrees(degrees)))
                {
                    shell.WriteError("Failed changing decal rotation.");
                }
                break;
            case "zindex":
                if(args.Length != 4)
                {
                    shell.WriteError("Expected 5 arguments.");
                    return;
                }

                if (!int.TryParse(args[3], out var zIndex))
                {
                    shell.WriteError("Failed parsing zIndex.");
                    return;
                }

                if (!decalSystem.SetDecalZIndex(gridId, uid, zIndex))
                {
                    shell.WriteError("Failed changing decal zIndex.");
                }
                break;
            case "clean":
                if(args.Length != 4)
                {
                    shell.WriteError("Expected 5 arguments.");
                    return;
                }

                if (!bool.TryParse(args[3], out var cleanable))
                {
                    shell.WriteError("Failed parsing cleanable.");
                    return;
                }

                if (!decalSystem.SetDecalCleanable(gridId, uid, cleanable))
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
