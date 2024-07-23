using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Content.Shared.Verbs;
using Robust.Shared.Console;

namespace Content.Server.Verbs.Commands
{
    [AdminCommand(AdminFlags.Moderator)]
    public sealed class ListVerbsCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public string Command => "listverbs";
        public string Description => Loc.GetString("list-verbs-command-description");
        public string Help => Loc.GetString("list-verbs-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine(Loc.GetString("list-verbs-command-invalid-args"));
                return;
            }

            var verbSystem = _entManager.System<SharedVerbSystem>();

            // get the 'player' entity (defaulting to command user, otherwise uses a uid)
            EntityUid? playerEntity = null;

            if (!int.TryParse(args[0], out var intPlayerUid))
            {
                if (args[0] == "self" && shell.Player?.AttachedEntity != null)
                {
                    playerEntity = shell.Player.AttachedEntity;
                }
                else
                {
                    shell.WriteError(Loc.GetString("list-verbs-command-invalid-player-uid"));
                    return;
                }
            }
            else
            {
                _entManager.TryGetEntity(new NetEntity(intPlayerUid), out playerEntity);
            }

            // gets the target entity
            if (!int.TryParse(args[1], out var intUid))
            {
                shell.WriteError(Loc.GetString("list-verbs-command-invalid-target-uid"));
                return;
            }

            if (playerEntity == null)
            {
                shell.WriteError(Loc.GetString("list-verbs-command-invalid-player-entity"));
                return;
            }

            var targetNet = new NetEntity(intUid);

            if (!_entManager.TryGetEntity(targetNet, out var target))
            {
                shell.WriteError(Loc.GetString("list-verbs-command-invalid-target-entity"));
                return;
            }

            var verbs = verbSystem.GetLocalVerbs(target.Value, playerEntity.Value, Verb.VerbTypes);

            foreach (var verb in verbs)
            {
                shell.WriteLine(Loc.GetString("list-verbs-verb-listing", ("type", verb.GetType().Name), ("verb", verb.Text)));
            }
        }
    }
}
