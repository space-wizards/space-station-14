using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Commands.Surgery
{
    [AdminCommand(AdminFlags.Spawn)]
    public class SurgeryToolsCommand : IConsoleCommand
    {
        public string Command => "surgerytools";
        public string Description => "Spawns surgery tools around you";
        public string Help => $"Usage: {Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 0)
            {
                shell.WriteError($"Invalid arguments.\n{Help}");
                return;
            }

            if (shell.Player is not IPlayerSession player)
            {
                shell.WriteError("You must be a player to run this command.");
                return;
            }

            if (player.AttachedEntity == null)
            {
                shell.WriteError("You must have an entity to run this command.");
                return;
            }

            var entities = IoCManager.Resolve<IPrototypeManager>().Index<EntityListPrototype>("SurgeryCommandTools").Entities;
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var i = 0;

            foreach (var prototype in entities)
            {
                entityManager.SpawnEntity(prototype.ID, player.AttachedEntity.Transform.Coordinates);
                i++;
            }

            shell.WriteLine($"Spawned {i} tools.");
        }
    }
}
