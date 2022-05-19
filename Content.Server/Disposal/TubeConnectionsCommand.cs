using Content.Server.Administration;
using Content.Server.Disposal.Tube.Components;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Disposal
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class TubeConnectionsCommand : IConsoleCommand
    {
        public string Command => "tubeconnections";
        public string Description => Loc.GetString("tube-connections-command-description");
        public string Help => Loc.GetString("tube-connections-command-help-text", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player?.AttachedEntity == null)
            {
                shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
                return;
            }

            if (args.Length < 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!EntityUid.TryParse(args[0], out var id))
            {
                shell.WriteLine(Loc.GetString("shell-invalid-entity-uid",("uid", args[0])));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            if (!entityManager.EntityExists(id))
            {
                shell.WriteLine(Loc.GetString("shell-could-not-find-entity-with-uid",("uid", id)));
                return;
            }

            if (!entityManager.TryGetComponent(id, out IDisposalTubeComponent? tube))
            {
                shell.WriteLine(Loc.GetString("shell-entity-with-uid-lacks-component",
                                              ("uid", id),
                                              ("componentName", nameof(IDisposalTubeComponent))));
                return;
            }

            tube.PopupDirections(player.AttachedEntity.Value);
        }
    }
}
