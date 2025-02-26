using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    sealed class SwapMindCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

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

            if (!int.TryParse(args[0], out var firstEntInt) || !int.TryParse(args[1], out var secondEntInt))
            {
                shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            var firstNetEnt = new NetEntity(firstEntInt);
            var secondNetEnt = new NetEntity(secondEntInt);

            if (!_entManager.TryGetEntity(firstNetEnt, out var firstEntityUid) ||
                !_entManager.TryGetEntity(secondNetEnt, out var secondEntityUid))
            {
                shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
                return;
            }

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
    }
}
