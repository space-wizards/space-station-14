using Content.Server.Administration;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Shuttles.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed class DockCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public string Command => "dock";
    public string Description => Loc.GetString("cmd-dock-desc");
    public string Help => Loc.GetString("cmd-dock-help");
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("cmd-dock-args"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var airlock1Net) || !_entManager.TryGetEntity(airlock1Net, out var airlock1))
        {
            shell.WriteError(Loc.GetString("cmd-dock-invalid", ("entity", args[0])));
            return;
        }

        if (!NetEntity.TryParse(args[1], out var airlock2Net) || !_entManager.TryGetEntity(airlock2Net, out var airlock2))
        {
            shell.WriteError(Loc.GetString("cmd-dock-invalid", ("entity", args[1])));
            return;
        }

        if (!_entManager.TryGetComponent(airlock1, out DockingComponent? dock1))
        {
            shell.WriteError(Loc.GetString("cmd-dock-found", ("airlock", airlock1)));
            return;
        }

        if (!_entManager.TryGetComponent(airlock2, out DockingComponent? dock2))
        {
            shell.WriteError(Loc.GetString("cmd-dock-found", ("airlock", airlock2)));
            return;
        }

        var dockSystem = _entManager.System<DockingSystem>();
        dockSystem.Dock((airlock1.Value, dock1), (airlock2.Value, dock2));

        if (dock1.DockedWith == airlock2)
        {
            shell.WriteLine(Loc.GetString("cmd-dock-success"));
        }
        else
        {
            shell.WriteError(Loc.GetString("cmd-dock-fail"));
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromOptions(CompletionHelper.Components<DockingComponent>(args[0], _entManager));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromOptions(CompletionHelper.Components<DockingComponent>(args[1], _entManager));
        }

        return CompletionResult.Empty;
    }
}
