#nullable enable
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public class SetAnchorCommand : IClientCommand
    {
        public string Command => "setanchor";
        public string Description => "Sets the anchoring state of an entity.";
        public string Help => "setanchor <entity id> <value (optional)>";
        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length == 0 || args.Length > 2)
            {
                shell.SendText(player, "Invalid number of argument!");
                return;
            }

            if (!int.TryParse(args[0], out var id))
            {
                shell.SendText(player, "Invalid argument specified!");
                return;
            }

            var entId = new EntityUid(id);

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.TryGetEntity(entId, out var entity) || entity.Deleted || !entity.TryGetComponent(out PhysicsComponent? physics))
            {
                shell.SendText(player, "Invalid entity specified!");
                return;
            }

            if (args.Length == 2)
            {
                if (!bool.TryParse(args[1], out var value))
                {
                    shell.SendText(player, "Invalid argument specified!");
                    return;
                }

                physics.Anchored = value;
                return;
            }

            physics.Anchored = !physics.Anchored;
        }
    }
}
