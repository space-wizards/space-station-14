using Content.Server.Administration;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Shuttles.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed class DockCommand : LocalizedEntityCommands
{
    [Dependency] private readonly DockingSystem _dockSystem = default!;

    public override string Command => "dock";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific",
                ("properAmount", 2),
                ("currentAmount", args.Length)));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var airlock1Net) || !EntityManager.TryGetEntity(airlock1Net, out var airlock1))
        {
            shell.WriteError(Loc.GetString("shell-invalid-entity-uid", ("uid", args[0])));
            return;
        }

        if (!NetEntity.TryParse(args[1], out var airlock2Net) || !EntityManager.TryGetEntity(airlock2Net, out var airlock2))
        {
            shell.WriteError(Loc.GetString("shell-invalid-entity-uid", ("uid", args[1])));
            return;
        }

        if (!EntityManager.TryGetComponent(airlock1, out DockingComponent? dock1))
        {
            shell.WriteError(Loc.GetString("shell-entity-with-uid-lacks-component", ("uid", args[0]), ("componentName", nameof(DockingComponent))));
            return;
        }

        if (!EntityManager.TryGetComponent(airlock2, out DockingComponent? dock2))
        {
            shell.WriteError(Loc.GetString("shell-entity-with-uid-lacks-component", ("uid", args[1]), ("componentName", nameof(DockingComponent))));
            return;
        }

        _dockSystem.Dock((airlock1.Value, dock1), (airlock2.Value, dock2));

        if (dock1.DockedWith == airlock2)
            shell.WriteLine(Loc.GetString("cmd-dock-success"));
        else
            shell.WriteError(Loc.GetString("cmd-dock-fail"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromOptions(CompletionHelper.Components<DockingComponent>(args[0], EntityManager)),
            2 => CompletionResult.FromOptions(CompletionHelper.Components<DockingComponent>(args[1], EntityManager)),
            _ => CompletionResult.Empty,
        };
    }
}
