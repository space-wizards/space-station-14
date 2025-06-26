using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class SetMindCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;

        public override string Command => "setmind";

        public override string Description => Loc.GetString("cmd-setmind-desc", ("requiredComponent", nameof(MindContainerComponent)));

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
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

            var ghostOverride = true;
            if (args.Length > 2)
            {
                ghostOverride = bool.Parse(args[2]);
            }

            var nent = new NetEntity(entInt);

            if (!EntityManager.TryGetEntity(nent, out var eUid))
            {
                shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
                return;
            }

            if (!EntityManager.HasComponent<MindContainerComponent>(eUid))
            {
                shell.WriteLine(Loc.GetString("cmd-setmind-target-has-no-mind-message"));
                return;
            }

            if (!_playerManager.TryGetSessionByUsername(args[1], out var session))
            {
                shell.WriteLine(Loc.GetString("shell-target-player-does-not-exist"));
                return;
            }

            // hm, does player have a mind? if not we may need to give them one
            var playerCData = session.ContentData();
            if (playerCData == null)
            {
                shell.WriteLine(Loc.GetString("cmd-setmind-target-has-no-content-data-message"));
                return;
            }

            var metadata = EntityManager.GetComponent<MetaDataComponent>(eUid.Value);

            var mind = playerCData.Mind ?? _mindSystem.CreateMind(session.UserId, metadata.EntityName);

            _mindSystem.TransferTo(mind, eUid, ghostOverride);
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 2)
                return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Help);

            return CompletionResult.Empty;
        }
    }
}
