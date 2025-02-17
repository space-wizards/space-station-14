using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class SetMindCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly ISharedPlayerManager _players = default!;

        public override string Command => "setmind";
        public override string Description => Loc.GetString("set-mind-command-description", ("requiredComponent", nameof(MindContainerComponent)));
        public override string Help => Loc.GetString("set-mind-command-help-text", ("command", Command));

        public override async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
            {
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!int.TryParse(args[0], out var entInt))
            {
                shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            bool ghostOverride = true;
            if (args.Length > 2)
            {
                ghostOverride = bool.Parse(args[2]);
            }

            var nent = new NetEntity(entInt);

            if (!_entManager.TryGetEntity(nent, out var eUid))
            {
                shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
                return;
            }

            if (!_entManager.HasComponent<MindContainerComponent>(eUid))
            {
                shell.WriteLine(Loc.GetString("set-mind-command-target-has-no-mind-message"));
                return;
            }

            if (!IoCManager.Resolve<IPlayerManager>().TryGetSessionByUsername(args[1], out var session))
            {
                shell.WriteLine(Loc.GetString("shell-target-player-does-not-exist"));
                return;
            }

            // hm, does player have a mind? if not we may need to give them one
            var playerCData = session.ContentData();
            if (playerCData == null)
            {
                shell.WriteLine(Loc.GetString("set-mind-command-target-has-no-content-data-message"));
                return;
            }

            var mindSystem = _entManager.System<SharedMindSystem>();
            var metadata = _entManager.GetComponent<MetaDataComponent>(eUid.Value);

            var mind = playerCData.Mind ?? mindSystem.CreateMind(session.UserId, metadata.EntityName);

            mindSystem.TransferTo(mind, eUid, ghostOverride);
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            // Complete copy-past from command "tpto", but why not?
            if (args.Length == 1)
            {
                if (args.Length == 0)
                    return CompletionResult.Empty;

                var last = args[^1];

                var users = _players.Sessions
                    .Select(x => x.Name ?? string.Empty)
                    .Where(x => !string.IsNullOrWhiteSpace(x) && x.StartsWith(last, StringComparison.CurrentCultureIgnoreCase));

                var hint = "set-mind-command-hint-entity";
                hint = Loc.GetString(hint);

                var opts = CompletionResult.FromHintOptions(users, hint);
                if (last != string.Empty && !NetEntity.TryParse(last, out _))
                    return opts;

                return CompletionResult.FromHintOptions(opts.Options.Concat(CompletionHelper.NetEntities(last, _entManager)), hint);
            }

            if (args.Length == 2)
            {
                return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), LocalizationManager.GetString("set-mind-command-hint-player"));
            }

            return CompletionResult.Empty;
        }
    }
}
