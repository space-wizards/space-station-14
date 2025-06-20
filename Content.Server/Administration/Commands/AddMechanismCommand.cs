using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AddMechanismCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly BodySystem _bodySystem = default!;

        public override string Command => "addmechanism";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number-need-specific",
                    ("$properAmount", 2),
                    ("currentAmount", args.Length)));
                return;
            }

            if (!NetEntity.TryParse(args[0], out var organIdNet) || !EntityManager.TryGetEntity(organIdNet, out var organId))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            if (!NetEntity.TryParse(args[1], out var partIdNet) || !EntityManager.TryGetEntity(partIdNet, out var partId))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            shell.WriteLine(_bodySystem.AddOrganToFirstValidSlot(partId.Value, organId.Value)
                ? Loc.GetString($"shell-child-attached-to-parent", ("child", organId), ("parent", partId))
                : Loc.GetString($"shell-failed-attach-child-to-parent", ("child", organId), ("parent", partId)));
        }
    }
}
