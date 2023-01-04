using Content.Shared.Administration;
using Content.Shared.Tag;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class AddTagCommand : LocalizedCommands
    {
        public override string Command => "addtag";
        public override string Description => "Adds a tag to a given entity";
        public override string Help => "Usage: addtag <entity uid> <tag>";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
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
            var tagSystem = entityManager.System<TagSystem>();

            if (tagSystem.TryAddTag(entityUid, args[1]))
            {
                shell.WriteLine($@"Added {args[1]} to {entityUid}.");
            }
            else
            {
                shell.WriteError($@"Could not add {args[1]} to {entityUid}.");
            }
        }
    }

    [AdminCommand(AdminFlags.Debug)]
    public sealed class RemoveTagCommand : LocalizedCommands
    {
        public override string Command => "removetag";
        public override string Description => "Removes a tag from a given entity";
        public override string Help => "Usage: removetag <entity uid> <tag>";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
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
            var tagSystem = entityManager.System<TagSystem>();

            if (tagSystem.RemoveTag(entityUid, args[1]))
            {
                shell.WriteLine($@"Removed {args[1]} from {entityUid}.");
            }
            else
            {
                shell.WriteError($@"Could not remove {args[1]} from {entityUid}.");
            }
        }
    }
}
