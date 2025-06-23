using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server.Verbs.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class ListVerbsCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedVerbSystem _verbSystem = default!;

    public override string Command => "listverbs";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number-specific",
                ("properAmount", 2),
                ("currentAmount", args.Length)));
            return;
        }

        // get the 'player' entity (defaulting to command user, otherwise uses a uid)
        ICommonSession? session;
        if (args[0] == "self")
            session = shell.Player;
        else
            _playerManager.TryGetSessionByUsername(args[0], out session);

        if (session?.AttachedEntity is not { } user)
        {
            shell.WriteLine(Loc.GetString("shell-target-player-does-not-exist"));
            return;
        }

        // gets the target entity
        if (!int.TryParse(args[1], out var intUid))
        {
            shell.WriteError(Loc.GetString("list-verbs-command-invalid-target-uid"));
            return;
        }

        var targetNet = new NetEntity(intUid);

        if (!EntityManager.TryGetEntity(targetNet, out var target))
        {
            shell.WriteError(Loc.GetString("list-verbs-command-invalid-target-entity"));
            return;
        }

        var verbs = _verbSystem.GetLocalVerbs(target.Value, user, Verb.VerbTypes);

        foreach (var verb in verbs)
        {
            shell.WriteLine(Loc.GetString("list-verbs-verb-listing", ("type", verb.GetType().Name), ("verb", verb.Text)));
        }
    }
}
