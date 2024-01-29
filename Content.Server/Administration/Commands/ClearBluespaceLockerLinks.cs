using Content.Server.Storage.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class ClearBluespaceLockerLinks : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

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

        if (!NetEntity.TryParse(args[0], out var entityUidNet) || !_entityManager.TryGetEntity(entityUidNet, out var entityUid))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        _entityManager.RemoveComponent<BluespaceLockerComponent>(entityUid.Value);
    }
}
