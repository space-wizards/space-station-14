using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Movement.Systems;
using Robust.Shared.Console;

namespace Content.Server.Movement;

[AdminCommand(AdminFlags.Fun)]
public sealed class LockEyesCommand : IConsoleCommand
{
    public string Command => $"lockeyes";
    public string Description => "Prevents eyes from being rotated any further";
    public string Help => $"{Command} <true/false>";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!bool.TryParse(args[0], out var value))
        {
            shell.WriteError($"Unable to parse {args[0]} as a bool");
            return;
        }

        var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedMoverController>();
        system.CameraRotationLocked = value;
    }
}
