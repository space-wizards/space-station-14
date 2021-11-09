using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Verbs;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Verbs.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public class ListVerbsCommand : IConsoleCommand
    {
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

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var verbSystem = EntitySystem.Get<SharedVerbSystem>();

            // get the 'player' entity (defaulting to command user, otherwise uses a uid)
            IEntity? playerEntity = null;
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
                entityManager.TryGetEntity(new EntityUid(intPlayerUid), out playerEntity);
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

            var entUid = new EntityUid(intUid);
            if (!entityManager.TryGetEntity(entUid, out var target))
            {
                shell.WriteError(Loc.GetString("list-verbs-command-invalid-target-entity"));
                return;
            }

            var verbs = verbSystem.GetLocalVerbs(
                target, playerEntity, VerbType.All, true
            );

            foreach (var (type, set) in verbs)
            {
                foreach (var verb in set)
                {
                    shell.WriteLine(Loc.GetString("list-verbs-verb-listing", ("type", type), ("verb", verb.Text)));
                }
            }
        }
    }
}
