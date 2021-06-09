#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Damage
{
    [AdminCommand(AdminFlags.Admin)]
    public class GodModeCommand : IConsoleCommand
    {
        public string Command => "godmode";
        public string Description => "Makes your entity or another invulnerable to almost anything. May have irreversible changes.";
        public string Help => $"Usage: {Command} / {Command} <entityUid>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            IEntity entity;

            switch (args.Length)
            {
                case 0:
                    if (player == null)
                    {
                        shell.WriteLine("An entity needs to be specified when the command isn't used by a player.");
                        return;
                    }

                    if (player.AttachedEntity == null)
                    {
                        shell.WriteLine("An entity needs to be specified when you aren't attached to an entity.");
                        return;
                    }

                    entity = player.AttachedEntity;
                    break;
                case 1:
                    if (!EntityUid.TryParse(args[0], out var id))
                    {
                        shell.WriteLine($"{args[0]} isn't a valid entity id.");
                        return;
                    }

                    var entityManager = IoCManager.Resolve<IEntityManager>();
                    if (!entityManager.TryGetEntity(id, out var parsedEntity))
                    {
                        shell.WriteLine($"No entity found with id {id}.");
                        return;
                    }

                    entity = parsedEntity;
                    break;
                default:
                    shell.WriteLine(Help);
                    return;
            }

            var godmodeSystem = EntitySystem.Get<GodmodeSystem>();
            var enabled = godmodeSystem.ToggleGodmode(entity);

            shell.WriteLine(enabled
                ? $"Enabled godmode for entity {entity.Name} with id {entity.Uid}"
                : $"Disabled godmode for entity {entity.Name} with id {entity.Uid}");
        }
    }
}
