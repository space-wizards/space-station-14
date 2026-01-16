using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class RemoveBodyPartCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public override string Command => "rmbodypart";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
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

            // TODO: THIS IS JUST A MECHANISM COPYPASTE
            var xformSystem = _entManager.System<SharedTransformSystem>();
            xformSystem.AttachToGridOrMap(entityUid.Value);
            shell.WriteLine(Loc.GetString("cmd-rmbodypart-removed", ("entityUid", entityUid.Value)));
        }
    }
}
