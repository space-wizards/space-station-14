using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Dataset;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class RandomDisplayNameCommand : LocalizedCommands
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    [ValidatePrototypeId<LocalizedDatasetPrototype>]
    public const string AdminNamesPrototypeId = "NamesAdmin";

    public override string Command => "randomizedisplayname";
    public override string Description => Loc.GetString("cmd-displayname-randomize-description");
    public override string Help => Loc.GetString("cmd-displayname-randomize-help");

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var names = _playerManager.Sessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();
            return CompletionResult.FromHintOptions(names, Loc.GetString("shell-argument-username-optional-hint"));
        }

        return CompletionResult.Empty;
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var player = shell.Player;
        if (player == null)
        {
            // If you are not a player, you require a player argument.
            if (args.Length == 0)
            {
                shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
                return;
            }

            var didFind = _playerManager.TryGetSessionByUsername(args[0], out player);
            if (!didFind)
            {
                shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
                return;
            }
        }

        // If you are a player and a username is provided, a lookup is done to find the target player.
        if (args.Length == 1)
        {
            var didFind = _playerManager.TryGetSessionByUsername(args[0], out player);
            if (!didFind)
            {
                shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
                return;
            }
        }

        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        if (!protoMan.TryIndex(AdminNamesPrototypeId, out LocalizedDatasetPrototype? nameData))
        {
            shell.WriteError(Loc.GetString("cmd-displayname-proto-fail", ("id", AdminNamesPrototypeId)));
            return;
        }

        var list = nameData.Values.Select(c => Loc.GetString(c)).Except(_playerManager.Sessions.Select(c => c.DisplayName)).ToList();

        if (list.Count == 0)
        {
            shell.WriteError(Loc.GetString("cmd-displayname-all-names-taken"));
            return;
        }

        _playerManager.SetDisplayName(player!, _random.Pick(list));
    }
}
