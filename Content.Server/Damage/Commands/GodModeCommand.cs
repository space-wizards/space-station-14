using Content.Server.Administration;
using Content.Server.Damage.Systems;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Damage.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class GodModeCommand : IConsoleCommand
    {
        public string Command => "godmode";
        public string Description => "Makes your entity or another invulnerable to almost anything. May have irreversible changes.";
        public string Help => $"Usage: {Command} / {Command} <entityUid>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            EntityUid entity;

            var entityManager = IoCManager.Resolve<IEntityManager>();

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

                    entity = player.AttachedEntity.Value;
                    break;
                case 1:
                    if (!EntityUid.TryParse(args[0], out var id))
                    {
                        shell.WriteLine($"{args[0]} isn't a valid entity id.");
                        return;
                    }

                    if (!entityManager.EntityExists(id))
                    {
                        shell.WriteLine($"No entity found with id {id}.");
                        return;
                    }

                    entity = id;
                    break;
                default:
                    shell.WriteLine(Help);
                    return;
            }

            var godmodeSystem = EntitySystem.Get<GodmodeSystem>();
            var enabled = godmodeSystem.ToggleGodmode(entity);

            var name = entityManager.GetComponent<MetaDataComponent>(entity).EntityName;

            shell.WriteLine(enabled
                ? $"Enabled godmode for entity {name} with id {entity}"
                : $"Disabled godmode for entity {name} with id {entity}");
        }
    }
}
