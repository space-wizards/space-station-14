using Content.Server.GameObjects;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration
{
    class Rejuvenate : IClientCommand
    {
        public string Command => "rejuvenate";
        public string Description
        {
            get
            {
                var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                return localizationManager.GetString("Fully heals a mob.");
            }
        }
        public string Help
        {
            get
            {
                var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                return localizationManager.GetString("Usage: rejuvenate <mobUid_1> <mobUid_2> ... <mobUid_n>\nAttempts to heal the user's mob if no arguments are provided.");
            }
        }

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var localizationManager = IoCManager.Resolve<ILocalizationManager>();
            if (args.Length < 1 && player != null) //Try to heal the users mob if applicable
            {
                shell.SendText(player, localizationManager.GetString("Healing the user's mob since no arguments were provided."));
                if (player.AttachedEntity == null)
                {
                    shell.SendText(player, localizationManager.GetString("There's no entity attached to the user."));
                    return;
                }
                if (!player.AttachedEntity.TryGetComponent(out DamageableComponent damage))
                {
                    shell.SendText(player, localizationManager.GetString("The user's entity does not have a DamageableComponent."));
                    return;
                }
                damage.HealAllDamage();
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            foreach (var arg in args)
            {
                if(!EntityUid.TryParse(arg, out var uid) || !entityManager.TryGetEntity(uid, out var entity))
                {
                    shell.SendText(player, localizationManager.GetString("Could not find entity {0}", arg));
                    continue;
                }
                if (!entity.TryGetComponent(out DamageableComponent damage))
                {
                    shell.SendText(player, localizationManager.GetString("Entity {0} does not have a DamageableComponent.", arg));
                    continue;
                }
                damage.HealAllDamage();
            }
        }
    }
}
