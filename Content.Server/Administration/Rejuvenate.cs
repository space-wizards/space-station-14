using Content.Server.GlobalVerbs;
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
                return Loc.GetString("Fully heals a mob.");
            }
        }
        public string Help
        {
            get
            {
                return Loc.GetString("Usage: rejuvenate <mobUid_1> <mobUid_2> ... <mobUid_n>\nAttempts to heal the user's mob if no arguments are provided.");
            }
        }

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length < 1 && player != null) //Try to heal the users mob if applicable
            {
                shell.SendText(player, Loc.GetString("Healing the user's mob since no arguments were provided."));
                if (player.AttachedEntity == null)
                {
                    shell.SendText(player, Loc.GetString("There's no entity attached to the user."));
                    return;
                }
                RejuvenateVerb.PerformRejuvenate(player.AttachedEntity);
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            foreach (var arg in args)
            {
                if(!EntityUid.TryParse(arg, out var uid) || !entityManager.TryGetEntity(uid, out var entity))
                {
                    shell.SendText(player, Loc.GetString("Could not find entity {0}", arg));
                    continue;
                }
                RejuvenateVerb.PerformRejuvenate(entity);
            }
        }
    }
}
