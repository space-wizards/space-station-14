using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Movement.Components;
using Robust.Shared.Console;

namespace Content.Server.Movement;

[AdminCommand(AdminFlags.Fun)]
public sealed class RotateEyesCommand : IConsoleCommand
{
    public string Command => "rotateeyes";
    public string Description => Loc.GetString("rotateeyes-command-description");
    public string Help => Loc.GetString("rotateeyes-command-help");
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entManager = IoCManager.Resolve<IEntityManager>();
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
        var query = entManager.EntityQueryEnumerator<InputMoverComponent>();
        while (query.MoveNext(out var uid, out var mover))
        {
            if (mover.TargetRelativeRotation.Equals(rotation))
                continue;

            mover.TargetRelativeRotation = rotation;

            entManager.Dirty(uid, mover);
            count++;
        }

        shell.WriteLine(Loc.GetString("rotateeyes-command-count", ("count", count)));
    }
}
