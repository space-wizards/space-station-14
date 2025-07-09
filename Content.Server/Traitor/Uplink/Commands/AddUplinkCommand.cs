using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server.Traitor.Uplink.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AddUplinkCommand : LocalizedEntityCommands
{
    [Dependency] private readonly UplinkSystem _uplinkSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Command => "adduplink";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 3)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        ICommonSession? session;
        if (args.Length > 0)
        {
            // Get player entity
            if (!_playerManager.TryGetSessionByUsername(args[0], out session))
            {
                shell.WriteLine(Loc.GetString("shell-target-player-does-not-exist"));
                return;
            }
        }
        else
            session = shell.Player;

        if (session?.AttachedEntity is not { } user)
        {
            shell.WriteLine(Loc.GetString("add-uplink-command-error-1"));
            return;
        }

        // Get target item
        EntityUid? uplinkEntity = null;
        if (args.Length >= 2)
        {
            if (!int.TryParse(args[1], out var itemId))
            {
                shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            var eNet = new NetEntity(itemId);

            if (!EntityManager.TryGetEntity(eNet, out var eUid))
            {
                shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
                return;
            }

            uplinkEntity = eUid;
        }

        var isDiscounted = false;
        if (args.Length >= 3)
        {
            if (!bool.TryParse(args[2], out isDiscounted))
            {
                shell.WriteLine(Loc.GetString("shell-invalid-bool"));
                return;
            }
        }

        // Finally add uplink
        if (!_uplinkSystem.AddUplink(user, 20, uplinkEntity: uplinkEntity, giveDiscounts: isDiscounted))
            shell.WriteLine(Loc.GetString("add-uplink-command-error-2"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("add-uplink-command-completion-1")),
            2 => CompletionResult.FromHint(Loc.GetString("add-uplink-command-completion-2")),
            3 => CompletionResult.FromHint(Loc.GetString("add-uplink-command-completion-3")),
            _ => CompletionResult.Empty,
        };
    }
}
