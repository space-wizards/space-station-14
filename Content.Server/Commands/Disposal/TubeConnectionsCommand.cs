#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Disposal;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Commands.Disposal
{
    [AdminCommand(AdminFlags.Debug)]
    public class TubeConnectionsCommand : IConsoleCommand
    {
        public string Command => "tubeconnections";
        public string Description => Loc.GetString("Shows all the directions that a tube can connect in.");
        public string Help => $"Usage: {Command} <entityUid>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player?.AttachedEntity == null)
            {
                shell.WriteLine(Loc.GetString("Only players can use this command"));
                return;
            }

            if (args.Length < 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!EntityUid.TryParse(args[0], out var id))
            {
                shell.WriteLine(Loc.GetString("{0} isn't a valid entity uid", args[0]));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            if (!entityManager.TryGetEntity(id, out var entity))
            {
                shell.WriteLine(Loc.GetString("No entity exists with uid {0}", id));
                return;
            }

            if (!entity.TryGetComponent(out IDisposalTubeComponent? tube))
            {
                shell.WriteLine(Loc.GetString("Entity with uid {0} doesn't have a {1} component", id, nameof(IDisposalTubeComponent)));
                return;
            }

            tube.PopupDirections(player.AttachedEntity);
        }
    }
}
