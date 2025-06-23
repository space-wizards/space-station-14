using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server.Verbs.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class InvokeVerbCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedVerbSystem _verbSystem = default!;

    public override string Command => "invokeverb";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number-specific",
                ("properAmount", 3),
                ("currentAmount", args.Length)));
            return;
        }

        // get the 'player' entity (defaulting to command user, otherwise uses a uid)
        ICommonSession? session;
        if (args[0] == "self" && shell.Player?.AttachedEntity != null)
            session = shell.Player;
        else if (!_playerManager.TryGetSessionByUsername(args[0], out session))
            shell.WriteLine(Loc.GetString("shell-target-player-does-not-exist"));

        if (session?.AttachedEntity is not { } user)
        {
            shell.WriteLine(Loc.GetString("shell-target-player-does-not-exist"));
            return;
        }

        // gets the target entity
        if (!int.TryParse(args[1], out var intUid))
        {
            shell.WriteError(Loc.GetString("invoke-verb-command-invalid-target-uid"));
            return;
        }

        var targetNet = new NetEntity(intUid);

        if (!EntityManager.TryGetEntity(targetNet, out var target))
        {
            shell.WriteError(Loc.GetString("invoke-verb-command-invalid-target-entity"));
            return;
        }

        var verbName = args[2].ToLowerInvariant();
        var verbs = _verbSystem.GetLocalVerbs(target.Value, user, Verb.VerbTypes, true);

        // if the "verb name" is actually a verb-type, try run any verb of that type.
        var verbType = Verb.VerbTypes.FirstOrDefault(x => x.Name == verbName);
        if (verbType != null)
        {
            var verb = verbs.FirstOrDefault(v => v.GetType() == verbType);
            if (verb != null)
            {
                _verbSystem.ExecuteVerb(verb, user, target.Value, forced: true);
                shell.WriteLine(Loc.GetString("invoke-verb-command-success", ("verb", verbName), ("target", target), ("player", playerEntity)));
                return;
            }
        }

        foreach (var verb in verbs.Where(verb => verb.Text.Equals(verbName, StringComparison.InvariantCultureIgnoreCase)))
        {
            _verbSystem.ExecuteVerb(verb, user, target.Value, forced: true);
            shell.WriteLine(Loc.GetString("invoke-verb-command-success", ("verb", verb.Text), ("target", target), ("player", playerEntity)));
            return;
        }

        // found nothing
        shell.WriteError(Loc.GetString("invoke-verb-command-verb-not-found", ("verb", verbName), ("target", target)));
    }
}
