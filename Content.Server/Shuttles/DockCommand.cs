using Content.Server.Administration;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Shuttles;

[AdminCommand(AdminFlags.Mapping)]
public sealed class DockCommand : IConsoleCommand
{
    public string Command => "dock";
    public string Description => $"Attempts to dock 2 airlocks together. Doesn't check whether it is valid.";
    public string Help => $"{Command} <airlock entityuid1> <airlock entityuid2>";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError($"Invalid number of args supplied");
            return;
        }

        if (!EntityUid.TryParse(args[0], out var airlock1))
        {
            shell.WriteError($"Invalid EntityUid {args[0]}");
            return;
        }

        if (!EntityUid.TryParse(args[1], out var airlock2))
        {
            shell.WriteError($"Invalid EntityUid {args[1]}");
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();

        if (!entManager.TryGetComponent(airlock1, out DockingComponent? dock1))
        {
            shell.WriteError($"No docking component found on {airlock1}");
            return;
        }

        if (!entManager.TryGetComponent(airlock2, out DockingComponent? dock2))
        {
            shell.WriteError($"No docking component found on {airlock2}");
            return;
        }

        var dockSystem = EntitySystem.Get<DockingSystem>();
        dockSystem.Dock(dock1, dock2);
    }
}
