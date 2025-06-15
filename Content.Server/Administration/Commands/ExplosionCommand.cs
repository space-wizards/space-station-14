using Content.Server.Administration.UI;
using Content.Server.EUI;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Explosion;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Numerics;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class OpenExplosionEui : LocalizedCommands
{
    [Dependency] private readonly EuiManager _eui = default!;
    public override string Command => "explosionui";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteError(Loc.GetString($"shell-cannot-run-command-from-server"));
            return;
        }

        var ui = new SpawnExplosionEui();
        _eui.OpenEui(ui, player);
    }
}

[AdminCommand(AdminFlags.Fun)] // for the admin. Not so much for anyone else.
public sealed class ExplosionCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override string Command => "explosion";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0 || args.Length == 4 || args.Length > 7)
        {
            shell.WriteError(Loc.GetString($"shell-wrong-number-of-arguments"));
            return;
        }

        if (!float.TryParse(args[0], out var intensity))
        {
            shell.WriteError(Loc.GetString($"cmd-explosion-failed-to-parse-intensity", ("args", args[0])));
            return;
        }

        float slope = 5;
        if (args.Length > 1 && !float.TryParse(args[1], out slope))
        {
            shell.WriteError(Loc.GetString($"cmd-explosion-failed-to-parse-float", ("args", args[1])));
            return;
        }

        float maxIntensity = 100;
        if (args.Length > 2 && !float.TryParse(args[2], out maxIntensity))
        {
            shell.WriteError(Loc.GetString($"cmd-explosion-failed-to-parse-float", ("args", args[2])));
            return;
        }

        float x = 0, y = 0;
        if (args.Length > 4)
        {
            if (!float.TryParse(args[3], out x) ||
                !float.TryParse(args[4], out y))
            {
                shell.WriteError(Loc.GetString($"cmd-explosion-failed-to-parse-coords", ("args", (args[3], args[4]))));
                return;
            }
        }

        MapCoordinates coords;
        if (args.Length > 5)
        {
            if (!int.TryParse(args[5], out var parsed))
            {
                shell.WriteError(Loc.GetString($"cmd-explosion-failed-to-parse-map-id", ("args", args[5])));
                return;
            }
            coords = new MapCoordinates(new Vector2(x, y), new(parsed));
        }
        else
        {
            // attempt to find the player's current position
            if (!EntityManager.TryGetComponent(shell.Player?.AttachedEntity, out TransformComponent? xform))
            {
                shell.WriteError(Loc.GetString($"cmd-explosion-failed-to-get-coordinates"));
                return;
            }

            if (args.Length > 4)
                coords = new MapCoordinates(new Vector2(x, y), xform.MapID);
            else
                coords = _transform.GetMapCoordinates(shell.Player.AttachedEntity.Value, xform: xform);
        }

        ExplosionPrototype? type;
        if (args.Length > 6)
        {
            if (!_protoMan.TryIndex(args[6], out type))
            {
                shell.WriteError($"Unknown explosion prototype: {args[6]}");
                return;
            }
        }
        else if (!_protoMan.TryIndex(ExplosionSystem.DefaultExplosionPrototypeId, out type))
        {
            // no prototype was specified, so lets default to whichever one was defined first
            type = _protoMan.EnumeratePrototypes<ExplosionPrototype>().FirstOrDefault();

            if (type == null)
            {
                shell.WriteError(Loc.GetString($"cmd-explosion-proto-manager-has-no-explosion-prototype"));
                return;
            }
        }

        _explosion.QueueExplosion(coords, type.ID, intensity, slope, maxIntensity, null);
    }
}
