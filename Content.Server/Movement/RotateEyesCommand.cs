using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Movement.Components;
using Robust.Shared.Console;

namespace Content.Server.Movement;

[AdminCommand(AdminFlags.Fun)]
public sealed class RotateEyesCommand : IConsoleCommand
{
    public string Command => "rotate_eyes";
    public string Description => $"Rotates every player's current eye to the specified rotation";
    public string Help => $"{Command} <degrees (default 0)>.";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entManager = IoCManager.Resolve<IEntityManager>();
        var rotation = Angle.Zero;

        if (args.Length == 1)
        {
            if (!float.TryParse(args[0], out var degrees))
            {
                shell.WriteError($"Unable to parse {args[0]}");
                return;
            }

            rotation = Angle.FromDegrees(degrees);
        }

        foreach (var mover in entManager.EntityQuery<InputMoverComponent>(true))
        {
            mover.TargetRelativeRotation = rotation;
            shell.WriteLine($"Set {entManager.ToPrettyString(mover.Owner)} rotation to {rotation.Degrees} degrees");
        }
    }
}
