using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Movement.Systems;
using Robust.Shared.Console;

namespace Content.Server.Movement;

[AdminCommand(AdminFlags.Fun)]
public sealed class LockEyesCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SharedMoverController _controller = default!;

    public override string Command => $"lockeyes";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
            return;
        }

        if (!bool.TryParse(args[0], out var value))
        {
            shell.WriteError(Loc.GetString("parse-bool-fail", ("args", args[0])));
            return;
        }

        _controller.CameraRotationLocked = value;
    }
}
