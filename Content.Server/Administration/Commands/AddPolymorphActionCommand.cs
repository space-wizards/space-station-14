using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class AddPolymorphActionCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public string Command => "addpolymorphaction";

    public string Description => Loc.GetString("add-polymorph-action-command-description");

    public string Help => Loc.GetString("add-polymorph-action-command-help-text");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var entityUidNet) || !_entityManager.TryGetEntity(entityUidNet, out var entityUid))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        var polySystem = _entityManager.EntitySysManager.GetEntitySystem<PolymorphSystem>();

        _entityManager.EnsureComponent<PolymorphableComponent>(entityUid.Value);
        polySystem.CreatePolymorphAction(args[1], entityUid.Value);
    }
}
