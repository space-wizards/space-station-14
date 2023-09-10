using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class RemoveBodyPartCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public string Command => "rmbodypart";
        public string Description => "Removes a given entity from it's containing body, if any.";
        public string Help => "Usage: rmbodypart <uid>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!NetEntity.TryParse(args[0], out var entityUidNet) || !_entManager.TryGetEntity(entityUidNet, out var entityUid))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            var bodySystem = _entManager.System<BodySystem>();

            if (bodySystem.DropPart(entityUid))
            {
                shell.WriteLine($"Removed body part {_entManager.ToPrettyString(entityUid.Value)}.");
            }
            else
            {
                shell.WriteError("Was not a body part, or did not have a parent.");
            }
        }
    }
}
