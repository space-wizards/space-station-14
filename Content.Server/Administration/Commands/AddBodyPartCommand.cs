using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Content.Shared.Body.Part;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AddBodyPartCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly BodySystem _bodySystem = default!;

        public override string Command => "addbodypart";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 3)
            {
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number-need-specific",
                    ("$properAmount", 3),
                    ("currentAmount", args.Length)));
                return;
            }

            if (!NetEntity.TryParse(args[0], out var childNetId))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            if (!NetEntity.TryParse(args[1], out var parentNetId))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            var childId = EntityManager.GetEntity(childNetId);
            var parentId = EntityManager.GetEntity(parentNetId);

            if (!Enum.TryParse<BodyPartType>(args[3], out var partType))
            {
                shell.WriteError(Loc.GetString($"cmd-addbodypart-part-type-invalid", ("partType", args[3])));
                return;
            }

            shell.WriteLine(_bodySystem.TryCreatePartSlotAndAttach(parentId, args[2], childId, partType)
                ? Loc.GetString($"shell-child-attached-to-parent", ("child", childId), ("parent", childId))
                : Loc.GetString($"shell-failed-attach-child-to-parent", ("child", childId), ("parent", childId)));
        }
    }
}
