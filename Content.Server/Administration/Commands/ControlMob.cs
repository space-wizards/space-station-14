using Content.Shared.Administration;
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class ControlMob : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "controlmob";
        public string Description => Loc.GetString("control-mob-command-description");
        public string Help => Loc.GetString("control-mob-command-help-text");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not IPlayerSession player)
            {
                shell.WriteLine("shell-server-cannot");
                return;
            }

            if (args.Length != 1)
            {
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!int.TryParse(args[0], out var targetId))
            {
                shell.WriteLine(Loc.GetString("shell-argument-must-be-number"));
                return;
            }

            var target = new EntityUid(targetId);

            if (!target.IsValid() || !_entities.EntityExists(target))
            {
                shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
                return;
            }

            var mindSystem = _entities.System<SharedMindSystem>();
            if (!mindSystem.TryGetMind(target, out var mindId, out var mind))
            {
                shell.WriteLine(Loc.GetString("shell-entity-is-not-mob"));
                return;
            }

            mindSystem.TransferTo(mindId, target, mind: mind);
        }
    }
}
