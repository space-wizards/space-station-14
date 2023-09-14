using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class DirtyCommand : IConsoleCommand
{
    public string Command => "dirty";
    public string Description => "Marks all components on an entity as dirty, if not specified, dirties everything";
    public string Help => $"Usage: {Command} [entityUid]";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        switch (args.Length)
        {
            case 0:
                foreach (var entity in entityManager.GetEntities())
                {
                    DirtyAll(entityManager, entity);
                }
                break;
            case 1:
                if (!EntityUid.TryParse(args[0], out var parsedTarget))
                {
                    shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                    return;
                }
                DirtyAll(entityManager, parsedTarget);
                break;
            default:
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
                break;
        }
    }

    private static void DirtyAll(IEntityManager manager, EntityUid entityUid)
    {
        foreach (var component in manager.GetComponents(entityUid))
        {
            manager.Dirty((Component)component);
        }
    }
}
