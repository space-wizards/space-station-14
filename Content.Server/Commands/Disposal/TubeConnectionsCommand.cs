#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Disposal;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Commands.Disposal
{
    [AdminCommand(AdminFlags.Debug)]
    public class TubeConnectionsCommand : IClientCommand
    {
        public string Command => "tubeconnections";
        public string Description => Loc.GetString("Shows all the directions that a tube can connect in.");
        public string Help => $"Usage: {Command} <entityUid>";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player?.AttachedEntity == null)
            {
                shell.SendText(player, Loc.GetString("Only players can use this command"));
                return;
            }

            if (args.Length < 1)
            {
                shell.SendText(player, Help);
                return;
            }

            if (!EntityUid.TryParse(args[0], out var id))
            {
                shell.SendText(player, Loc.GetString("{0} isn't a valid entity uid", args[0]));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            if (!entityManager.TryGetEntity(id, out var entity))
            {
                shell.SendText(player, Loc.GetString("No entity exists with uid {0}", id));
                return;
            }

            if (!entity.TryGetComponent(out IDisposalTubeComponent? tube))
            {
                shell.SendText(player, Loc.GetString("Entity with uid {0} doesn't have a {1} component", id, nameof(IDisposalTubeComponent)));
                return;
            }

            tube.PopupDirections(player.AttachedEntity);
        }
    }
}
