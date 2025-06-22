using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class AddPolymorphActionCommand : LocalizedEntityCommands
{
    [Dependency] private readonly PolymorphSystem _polySystem = default!;

    public override string Command => "addpolymorphaction";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number-need-specific",
                ("$properAmount", 2),
                ("currentAmount", args.Length)));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var entityUidNet) || !EntityManager.TryGetEntity(entityUidNet, out var entityUid))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        var polymorphable = EntityManager.EnsureComponent<PolymorphableComponent>(entityUid.Value);
        _polySystem.CreatePolymorphAction(args[1], (entityUid.Value, polymorphable));
    }
}
