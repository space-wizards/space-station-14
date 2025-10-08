using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Content.Shared.Body.Part;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AddBodyPartCommand : LocalizedEntityCommands
{
    [Dependency] private readonly BodySystem _bodySystem = default!;

    public override string Command => "addbodypart";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 4)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var childNetId) || !EntityManager.TryGetEntity(childNetId, out var childId))
        {
            shell.WriteError(Loc.GetString("shell-invalid-entity-uid", ("uid", args[0])));
            return;
        }

        if (!NetEntity.TryParse(args[1], out var parentNetId) || !EntityManager.TryGetEntity(parentNetId, out var parentId))
        {
            shell.WriteError(Loc.GetString("shell-invalid-entity-uid", ("uid", args[1])));
            return;
        }

        if (Enum.TryParse<BodyPartType>(args[3], out var partType) &&
            _bodySystem.TryCreatePartSlotAndAttach(parentId.Value, args[2], childId.Value, partType))
        {
            shell.WriteLine($@"Added {childId} to {parentId}.");
        }
        else
            shell.WriteError($@"Could not add {childId} to {parentId}.");
    }
}
