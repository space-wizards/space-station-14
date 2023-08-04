using Content.Shared.Administration;
using Content.Shared.Tag;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class AddTagCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override string Command => "addtag";
        public override string Description => Loc.GetString("addtag-command-description");
        public override string Help => Loc.GetString("addtag-command-help");

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

            if (!_entityManager.TrySystem(out TagSystem? tagSystem))
                return;
            _entityManager.EnsureComponent<TagComponent>(entityUid);

            if (tagSystem.TryAddTag(entityUid, args[1]))
            {
                shell.WriteLine(Loc.GetString("addtag-command-success", ("tag", args[1]), ("target", entityUid)));
            }
            else
            {
                shell.WriteError(Loc.GetString("addtag-command-fail", ("tag", args[1]), ("target", entityUid)));
            }
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                return CompletionResult.FromHint(Loc.GetString("shell-argument-uid"));
            }

            if (args.Length == 2)
            {
                return CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<TagPrototype>(),
                    Loc.GetString("tag-command-arg-tag"));
            }

            return CompletionResult.Empty;
        }
    }

    [AdminCommand(AdminFlags.Debug)]
    public sealed class RemoveTagCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override string Command => "removetag";
        public override string Description => Loc.GetString("removetag-command-description");
        public override string Help => Loc.GetString("removetag-command-help");

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

            if (!_entityManager.TrySystem(out TagSystem? tagSystem))
                return;

            if (tagSystem.RemoveTag(entityUid, args[1]))
            {
                shell.WriteLine(Loc.GetString("removetag-command-success", ("tag", args[1]), ("target", entityUid)));
            }
            else
            {
                shell.WriteError(Loc.GetString("removetag-command-fail", ("tag", args[1]), ("target", entityUid)));
            }
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                return CompletionResult.FromHint(Loc.GetString("shell-argument-uid"));
            }

            if (args.Length == 2&& EntityUid.TryParse(args[0], out var entityUid) && _entityManager.TryGetComponent(entityUid, out TagComponent? tagComponent))
            {
                return CompletionResult.FromHintOptions(tagComponent.Tags,
                    Loc.GetString("tag-command-arg-tag"));
            }

            return CompletionResult.Empty;
        }
    }
}
