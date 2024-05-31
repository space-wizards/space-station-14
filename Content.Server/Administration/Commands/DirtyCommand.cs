using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class DirtyCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public string Command => "dirty";
    public string Description => "Marks all components on an entity as dirty, if not specified, dirties everything";
    public string Help => $"Usage: {Command} [entityUid]";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        switch (args.Length)
        {
            case 0:
                foreach (var entity in _entManager.GetEntities())
                {
                    DirtyAll(_entManager, entity);
                }
                break;
            case 1:
                if (!NetEntity.TryParse(args[0], out var parsedTarget))
                {
                    shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                    return;
                }
                DirtyAll(_entManager, _entManager.GetEntity(parsedTarget));
                break;
            default:
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
                break;
        }
    }

    private static void DirtyAll(IEntityManager manager, EntityUid entityUid)
    {
        foreach (var component in manager.GetNetComponents(entityUid))
        {
            manager.Dirty(entityUid, component.component);
        }
    }
}
