using Content.Server.Mind;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class ControlMob : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public override string Command => "controlmob";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
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

            var targetNet = new NetEntity(targetId);

            if (!_entities.TryGetEntity(targetNet, out var target))
            {
                shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
                return;
            }

            _entities.System<MindSystem>().ControlMob(player.UserId, target.Value);
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length != 1)
                return CompletionResult.Empty;

            return CompletionResult.FromOptions(CompletionHelper.NetEntities(args[0], entManager: _entities));
        }
    }
}
