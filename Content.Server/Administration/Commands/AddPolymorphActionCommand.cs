using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class AddPolymorphActionCommand : IConsoleCommand
{
    public string Command => "addpolymorphaction";

    public string Description => Loc.GetString("add-polymorph-action-command-description");

    public string Help => Loc.GetString("add-polymorph-action-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var entityUid))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        var entityManager = IoCManager.Resolve<IEntityManager>();
        var polySystem = entityManager.EntitySysManager.GetEntitySystem<PolymorphableSystem>();

        entityManager.EnsureComponent<PolymorphableComponent>(entityUid);
        polySystem.CreatePolymorphAction(args[1], entityUid);
    }
}
