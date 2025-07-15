using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Movement.Components;
using Robust.Shared.Console;

namespace Content.Server.Movement;

[AdminCommand(AdminFlags.Fun)]
public sealed class RotateEyesCommand : LocalizedEntityCommands
{
    public override string Command => "rotateeyes";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var rotation = Angle.Zero;

        if (args.Length == 1)
        {
            if (!float.TryParse(args[0], out var degrees))
            {
                shell.WriteError(Loc.GetString("parse-float-fail", ("arg", args[0])));
                return;
            }

            rotation = Angle.FromDegrees(degrees);
        }

        var count = 0;
        var query = EntityManager.EntityQueryEnumerator<InputMoverComponent>();
        while (query.MoveNext(out var uid, out var mover))
        {
            if (mover.TargetRelativeRotation.Equals(rotation))
                continue;

            mover.TargetRelativeRotation = rotation;

            EntityManager.Dirty(uid, mover);
            count++;
        }

        shell.WriteLine(Loc.GetString("cmd-rotateeyes-command-count", ("count", count)));
    }
}
