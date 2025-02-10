using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.FixedPoint;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server.Traitor.Uplink.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AddUplinkCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public string Command => "adduplink";

        public string Description => Loc.GetString("add-uplink-command-description");

        public string Help => Loc.GetString("add-uplink-command-help");


        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return args.Length switch
            {
                1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("add-uplink-command-completion-1")),
                2 => CompletionResult.FromHint(Loc.GetString("add-uplink-command-completion-2")),
                3 => CompletionResult.FromHint(Loc.GetString("add-uplink-command-completion-3")),
                _ => CompletionResult.Empty
            };
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
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
            {
                session = shell.Player;
            }

            if (session?.AttachedEntity is not { } user)
            {
                shell.WriteLine(Loc.GetString("add-uplink-command-error-1"));
                return;
            }

            // Get target item
            EntityUid? uplinkEntity = null;
            if (args.Length >= 2)
            {
                if (!int.TryParse(args[1], out var itemID))
                {
                    shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
                    return;
                }

                var eNet = new NetEntity(itemID);

                if (!_entManager.TryGetEntity(eNet, out var eUid))
                {
                    shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
                    return;
                }

                uplinkEntity = eUid;
            }

            bool isDiscounted = false;
            if (args.Length >= 3)
            {
                if (!bool.TryParse(args[2], out isDiscounted))
                {
                    shell.WriteLine(Loc.GetString("shell-invalid-bool"));
                    return;
                }
            }

            // Finally add uplink
            var uplinkSys = _entManager.System<UplinkSystem>();
            if (!uplinkSys.AddUplink(user, 20, uplinkEntity: uplinkEntity, giveDiscounts: isDiscounted))
            {
                shell.WriteLine(Loc.GetString("add-uplink-command-error-2"));
            }
        }
    }
}
