using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Spawn)]
public sealed class DeleteComponentCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;

    public override string Command => "deletecomponent";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteLine(Loc.GetString($"shell-need-exactly-one-argument"));
            return;
        }

        var name = string.Join(" ", args);

        if (!_compFactory.TryGetRegistration(name, out var registration))
        {
            shell.WriteLine(Loc.GetString($"cmd-deletecomponent-no-component-exists", ("name", name)));
            return;
        }

        var componentType = registration.Type;
        var components = EntityManager.GetAllComponents(componentType, true);

        var i = 0;

        foreach (var (uid, component) in components)
        {
            EntityManager.RemoveComponent(uid, component);
            i++;
        }

        shell.WriteLine(Loc.GetString($"cmd-deletecomponent-success", ("count", i), ("name", name)));
    }
}
