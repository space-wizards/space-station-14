using Content.Server.Storage.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class ClearBluespaceLockerLinks : IConsoleCommand
{
    public string Command => "clearbluespacelockerlinks";
    public string Description => "Removes the bluespace links of the given uid. Does not remove links this uid is the target of.";
    public string Help => "Usage: clearbluespacelockerlinks <storage uid>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
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

        if (entityManager.TryGetComponent<BluespaceLockerComponent>(entityUid, out var originComponent))
            entityManager.RemoveComponent(entityUid, originComponent);
    }
}
