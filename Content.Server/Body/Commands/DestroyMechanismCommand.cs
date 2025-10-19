using Content.Server.Administration;
using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Robust.Shared.Console;

namespace Content.Server.Body.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    internal sealed class DestroyMechanismCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IComponentFactory _compFactory = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;

        public override string Command => "destroymechanism";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine(Loc.GetString($"shell-only-players-can-run-this-command"));
                return;
            }

            if (args.Length == 0)
            {
                shell.WriteLine(Help);
                return;
            }

            if (player.AttachedEntity is not {} attached)
            {
                shell.WriteLine(Loc.GetString($"shell-must-be-attached-to-entity"));
                return;
            }

            if (!EntityManager.TryGetComponent(attached, out BodyComponent? body))
            {
                shell.WriteLine(Loc.GetString($"shell-must-have-body"));
                return;
            }

            var mechanismName = string.Join(" ", args).ToLowerInvariant();

            foreach (var organ in _bodySystem.GetBodyOrgans(attached, body))
            {
                if (_compFactory.GetComponentName(organ.Component.GetType()).ToLowerInvariant() == mechanismName)
                {
                    EntityManager.QueueDeleteEntity(organ.Id);
                    shell.WriteLine(Loc.GetString($"cmd-destroymechanism-success", ("name", mechanismName)));
                    return;
                }
            }

            shell.WriteLine(Loc.GetString($"cmd-destroymechanism-no-mechanism-found", ("name", mechanismName)));
        }
    }
}
