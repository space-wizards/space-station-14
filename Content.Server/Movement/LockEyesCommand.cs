using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Movement.Systems;
using Robust.Shared.Console;

namespace Content.Server.Movement;

[AdminCommand(AdminFlags.Fun)]
public sealed class LockEyesCommand : IConsoleCommand
{
    public string Command => $"lockeyes";
    public string Description => Loc.GetString("lockeyes-command-description");
    public string Help => Loc.GetString("lockeyes-command-help");
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!bool.TryParse(args[0], out var value))
        {
            shell.WriteError(Loc.GetString("parse-bool-fail", ("args", args[0])));
            return;
        }

        var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedMoverController>();
        system.CameraRotationLocked = value;
    }
}
