using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Server.Player;
using Robust.Shared.Console;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    sealed class SwapMindCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public string Command => "swapmind";

        public string Description => Loc.GetString("set-swapmind-command-description", ("requiredComponent", nameof(MindContainerComponent)));

        public string Help => Loc.GetString("set-swapmind-command-help-text", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
            {
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            // First player
            if (!TryParseUid(args[0], shell, _entManager, out var firstEntityUid))
                return;

            // Second player
            if (!TryParseUid(args[1], shell, _entManager, out var secondEntityUid))
                return;

            if (!_entManager.HasComponent<MindContainerComponent>(firstEntityUid) ||
                !_entManager.HasComponent<MindContainerComponent>(secondEntityUid))
            {
                shell.WriteLine(Loc.GetString("set-swapmind-command-target-has-no-mind-message"));
                return;
            }

            var mindSystem = _entManager.System<SharedMindSystem>();

            var firstMind = _entManager.GetComponent<MindContainerComponent>(firstEntityUid.Value).Mind;
            var secondMind = _entManager.GetComponent<MindContainerComponent>(secondEntityUid.Value).Mind;

            // Swap the minds
            if (firstMind != null && secondMind != null)
            {
                mindSystem.TransferTo(firstMind.Value, secondEntityUid);
                mindSystem.TransferTo(secondMind.Value, firstEntityUid);
                shell.WriteLine(Loc.GetString("set-swapmind-success-message"));
            }
            else
            {
                shell.WriteLine(Loc.GetString("set-swapmind-command-minds-not-found"));
            }
        }

        private bool TryParseUid(string str, IConsoleShell shell,
            IEntityManager entMan, [NotNullWhen(true)] out EntityUid? entityUid)
        {
            if (NetEntity.TryParse(str, out var entityUidNet) && _entManager.TryGetEntity(entityUidNet, out entityUid) && entMan.EntityExists(entityUid))
                return true;

            if (_playerManager.TryGetSessionByUsername(str, out var session) && session.AttachedEntity.HasValue)
            {
                entityUid = session.AttachedEntity.Value;
                return true;
            }

            if (session == null)
                shell.WriteError(Loc.GetString("cmd-rename-not-found", ("target", str)));
            else
                shell.WriteError(Loc.GetString("cmd-rename-no-entity", ("target", str)));

            entityUid = EntityUid.Invalid;
            return false;
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("cmd-mind-command-hint"));
            }

            if (args.Length == 2)
            {
                return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("cmd-mind-command-hint"));
            }

            return CompletionResult.Empty;
        }
    }
}
