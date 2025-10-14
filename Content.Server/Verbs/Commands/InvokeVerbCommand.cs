using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Verbs;
using Robust.Shared.Console;

namespace Content.Server.Verbs.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class InvokeVerbCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly SharedVerbSystem _verbSystem = default!;

        public override string Command => "invokeverb";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 3)
            {
                shell.WriteLine(Loc.GetString("cmd-invokeverb-invalid-args"));
                return;
            }

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
                    shell.WriteError(Loc.GetString("cmd-invokeverb-invalid-player-uid"));
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
                shell.WriteError(Loc.GetString("cmd-invokeverb-invalid-target-uid"));
                return;
            }

            if (playerEntity == null)
            {
                shell.WriteError(Loc.GetString("cmd-invokeverb-invalid-player-entity"));
                return;
            }

            var targetNet = new NetEntity(intUid);

            if (!EntityManager.TryGetEntity(targetNet, out var target))
            {
                shell.WriteError(Loc.GetString("cmd-invokeverb-invalid-target-entity"));
                return;
            }

            var verbName = args[2].ToLowerInvariant();
            var verbs = _verbSystem.GetLocalVerbs(target.Value, playerEntity.Value, Verb.VerbTypes, true);

            // if the "verb name" is actually a verb-type, try run any verb of that type.
            var verbType = Verb.VerbTypes.FirstOrDefault(x => x.Name == verbName);
            if (verbType != null)
            {
                var verb = verbs.FirstOrDefault(v => v.GetType() == verbType);
                if (verb != null)
                {
                    _verbSystem.ExecuteVerb(verb, playerEntity.Value, target.Value, forced: true);
                    shell.WriteLine(Loc.GetString("cmd-invokeverb-success", ("verb", verbName), ("target", target), ("player", playerEntity)));
                    return;
                }
            }

            foreach (var verb in verbs)
            {
                if (verb.Text.ToLowerInvariant() == verbName)
                {
                    _verbSystem.ExecuteVerb(verb, playerEntity.Value, target.Value, forced: true);
                    shell.WriteLine(Loc.GetString("cmd-invokeverb-success", ("verb", verb.Text), ("target", target), ("player", playerEntity)));
                    return;
                }
            }

            // found nothing
            shell.WriteError(Loc.GetString("cmd-invokeverb-verb-not-found", ("verb", verbName), ("target", target)));
        }
    }
}
