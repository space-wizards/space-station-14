using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server.Decals;

[AdminCommand(AdminFlags.Mapping)]
public sealed class EditDecalCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override string Command => "editdecal";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 4)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-arguments", ("minimum", 5)));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var gridIdNet) || !_entManager.TryGetEntity(gridIdNet, out var gridId))
        {
            shell.WriteError(Loc.GetString("cmd-editdecal-failed-parsing-gridId", ("gridId", args[3])));
            return;
        }

        if (!uint.TryParse(args[1], out var uid))
        {
            shell.WriteError(Loc.GetString("cmd-editdecal-failed-parsing-uid", ("uid", args[1])));
            return;
        }

        if (!_entManager.HasComponent<MapGridComponent>(gridId))
        {
            shell.WriteError(Loc.GetString("cmd-editdecal-no-grid-with-gridId", ("gridId", gridId)));
            return;
        }

        var decalSystem = _entManager.System<DecalSystem>();
        switch (args[2].ToLower())
        {
            case "position":
                if (args.Length != 5)
                {
                    shell.WriteError(Loc.GetString("shell-need-minimum-arguments", ("minimum", 6)));
                    return;
                }

                if (!float.TryParse(args[3], out var x) || !float.TryParse(args[4], out var y))
                {
                    shell.WriteError(Loc.GetString("cmd-editdecal-failed-parsing-position"));
                    return;
                }

                if (!decalSystem.SetDecalPosition(gridId.Value, uid, new(gridId.Value, new Vector2(x, y))))
                {
                    shell.WriteError(Loc.GetString("cmd-editdecal-failed-changing-decalposition"));
                }
                break;
            case "color":
                if (args.Length != 4)
                {
                    shell.WriteError(Loc.GetString("shell-need-minimum-arguments", ("minimum", 5)));
                    return;
                }

                if (!Color.TryFromName(args[3], out var color))
                {
                    shell.WriteError(Loc.GetString("cmd-editdecal-failed-parsing-color"));
                    return;
                }

                if (!decalSystem.SetDecalColor(gridId.Value, uid, color))
                {
                    shell.WriteError(Loc.GetString("cmd-editdecal-failed-changing-decal-color"));
                }
                break;
            case "id":
                if (args.Length != 4)
                {
                    shell.WriteError(Loc.GetString("shell-need-minimum-arguments", ("minimum", 5)));
                    return;
                }

                if (!decalSystem.SetDecalId(gridId.Value, uid, args[3]))
                {
                    shell.WriteError(Loc.GetString("cmd-editdecal-failed-changing-decal-id"));
                }
                break;
            case "rotation":
                if (args.Length != 4)
                {
                    shell.WriteError(Loc.GetString("cmd-editdecal-expected-arguments", ("expected", 5)));
                    return;
                }

                if (!double.TryParse(args[3], out var degrees))
                {
                    shell.WriteError(Loc.GetString("cmd-editdecal-failed-parsing-degrees"));
                    return;
                }

                if (!decalSystem.SetDecalRotation(gridId.Value, uid, Angle.FromDegrees(degrees)))
                {
                    shell.WriteError(Loc.GetString("cmd-editdecal-failed-changing-decal-rotation"));
                }
                break;
            case "zindex":
                if (args.Length != 4)
                {
                    shell.WriteError(Loc.GetString("shell-need-minimum-arguments", ("minimum", 5)));
                    return;
                }

                if (!int.TryParse(args[3], out var zIndex))
                {
                    shell.WriteError(Loc.GetString("cmd-editdecal-failed-parsing-zIndex"));
                    return;
                }

                if (!decalSystem.SetDecalZIndex(gridId.Value, uid, zIndex))
                {
                    shell.WriteError(Loc.GetString("cmd-editdecal-failed-changing-decal-zIndex"));
                }
                break;
            case "clean":
                if (args.Length != 4)
                {
                    shell.WriteError(Loc.GetString("shell-need-minimum-arguments", ("minimum", 5)));
                    return;
                }

                if (!bool.TryParse(args[3], out var cleanable))
                {
                    shell.WriteError(Loc.GetString("cmd-editdecal-failed-parsing-cleanable"));
                    return;
                }

                if (!decalSystem.SetDecalCleanable(gridId.Value, uid, cleanable))
                {
                    shell.WriteError(Loc.GetString("cmd-editdecal-failed-changing-decal-cleanable-flag"));
                }
                break;
            default:
                shell.WriteError(Loc.GetString("cmd-editdecal-invalid-mode"));
                return;
        }
    }
}
