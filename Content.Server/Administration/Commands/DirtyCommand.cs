using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class DirtyCommand : LocalizedEntityCommands
{
    public override string Command => "dirty";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        switch (args.Length)
        {
            case 0:
                foreach (var entity in EntityManager.GetEntities())
                {
                    DirtyAll(entity);
                }
                break;
            case 1:
                if (!NetEntity.TryParse(args[0], out var parsedTarget))
                {
                    shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                    return;
                }
                DirtyAll(EntityManager.GetEntity(parsedTarget));
                break;
            default:
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
                break;
        }
    }

    private void DirtyAll(EntityUid entityUid)
    {
        foreach (var component in EntityManager.GetNetComponents(entityUid))
        {
            EntityManager.Dirty(entityUid, component.component);
        }
    }
}
