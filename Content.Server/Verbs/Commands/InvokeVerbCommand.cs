using System;
using System.Linq;
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
    public class InvokeVerbCommand : IConsoleCommand
    {
        public string Command => "invokeverb";
        public string Description => Loc.GetString("invoke-verb-command-description");
        public string Help => Loc.GetString("invoke-verb-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 3)
            {
                shell.WriteLine(Loc.GetString("invoke-verb-command-invalid-args"));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var verbSystem = EntitySystem.Get<SharedVerbSystem>();

            // get the 'player' entity (defaulting to command user, otherwise uses a uid)
            EntityUid? playerEntity = null;
            if (!int.TryParse(args[0], out var intPlayerUid))
            {
                if (args[0] == "self" && shell.Player?.AttachedEntity != null)
                {
                    playerEntity = shell.Player.AttachedEntity.Value;
                }
                else
                {
                    shell.WriteError(Loc.GetString("invoke-verb-command-invalid-player-uid"));
                    return;
                }
            }
            else
            {
                entityManager.EntityExists(new EntityUid(intPlayerUid));
            }

            // gets the target entity
            if (!int.TryParse(args[1], out var intUid))
            {
                shell.WriteError(Loc.GetString("invoke-verb-command-invalid-target-uid"));
                return;
            }

            if (playerEntity == null)
            {
                shell.WriteError(Loc.GetString("invoke-verb-command-invalid-player-entity"));
                return;
            }

            var target = new EntityUid(intUid);
            if (!entityManager.EntityExists(target))
            {
                shell.WriteError(Loc.GetString("invoke-verb-command-invalid-target-entity"));
                return;
            }

            var verbName = args[2].ToLowerInvariant();
            var verbs = verbSystem.GetLocalVerbs(target, playerEntity.Value, VerbType.All, true);

            if ((Enum.TryParse(typeof(VerbType), verbName, ignoreCase: true, out var vtype) &&
                vtype is VerbType key) &&
                verbs.TryGetValue(key, out var vset) &&
                vset.Any())
            {
                verbSystem.ExecuteVerb(vset.First(), playerEntity.Value, target, forced: true);
                shell.WriteLine(Loc.GetString("invoke-verb-command-success", ("verb", verbName), ("target", target), ("player", playerEntity)));
                return;
            }

            foreach (var (_, set) in verbs)
            {
                foreach (var verb in set)
                {
                    if (verb.Text.ToLowerInvariant() == verbName)
                    {
                        verbSystem.ExecuteVerb(verb, playerEntity.Value, target, forced: true);
                        shell.WriteLine(Loc.GetString("invoke-verb-command-success", ("verb", verb.Text), ("target", target), ("player", playerEntity)));
                        return;
                    }
                }
            }

            // found nothing
            shell.WriteError(Loc.GetString("invoke-verb-command-verb-not-found", ("verb", verbName), ("target", target)));
        }
    }
}
