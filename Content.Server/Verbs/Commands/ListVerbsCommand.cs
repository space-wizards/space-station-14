using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Content.Shared.Verbs;
using Robust.Shared.Console;

namespace Content.Server.Verbs.Commands
{
    [AdminCommand(AdminFlags.Moderator)]
    public sealed class ListVerbsCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly SharedVerbSystem _verbSystem = default!;

        public override string Command => "listverbs";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine(Loc.GetString("cmd-listverbs-invalid-args"));
                return;
            }

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
                    shell.WriteError(Loc.GetString("cmd-listverbs-invalid-player-uid"));
                    return;
                }
            }
            else
            {
                EntityManager.TryGetEntity(new NetEntity(intPlayerUid), out playerEntity);
            }

            // gets the target entity
            if (!int.TryParse(args[1], out var intUid))
            {
                shell.WriteError(Loc.GetString("cmd-listverbs-invalid-target-uid"));
                return;
            }

            if (playerEntity == null)
            {
                shell.WriteError(Loc.GetString("cmd-listverbs-invalid-player-entity"));
                return;
            }

            var targetNet = new NetEntity(intUid);

            if (!EntityManager.TryGetEntity(targetNet, out var target))
            {
                shell.WriteError(Loc.GetString("cmd-listverbs-invalid-target-entity"));
                return;
            }

            var verbs = _verbSystem.GetLocalVerbs(target.Value, playerEntity.Value, Verb.VerbTypes);

            foreach (var verb in verbs)
            {
                shell.WriteLine(Loc.GetString("cmd-listverbs-verb-listing", ("type", verb.GetType().Name), ("verb", verb.Text)));
            }
        }
    }
}
