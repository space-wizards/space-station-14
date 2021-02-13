#nullable enable
using System.Linq;
using Content.Server.Administration;
using Content.Server.GameObjects.Components;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Interactable
{
    [AdminCommand(AdminFlags.Debug)]
    class UnAnchorCommand : IConsoleCommand
    {
        public string Command => "unanchor";
        public string Description => "Unanchors all anchorable entities in a radius around the user";
        public string Help => $"Usage: {Command} <radius>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player?.AttachedEntity == null)
            {
                return;
            }

            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!int.TryParse(args[0], out var radius))
            {
                shell.WriteLine($"{args[0]} isn't a valid integer.");
                return;
            }

            if (radius < 0)
            {
                shell.WriteLine("Radius must be positive.");
                return;
            }

            var serverEntityManager = IoCManager.Resolve<IServerEntityManager>();
            var entities = serverEntityManager.GetEntitiesInRange(player.AttachedEntity, radius).ToList();

            foreach (var entity in entities)
            {
                if (entity.TryGetComponent(out AnchorableComponent? anchorable))
                {
                    _ = anchorable.TryUnAnchor(player.AttachedEntity, force: true);
                }
            }
        }
    }
}
