using Content.Server.GlobalVerbs;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    class Rejuvenate : IConsoleCommand
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

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (args.Length < 1 && player != null) //Try to heal the users mob if applicable
            {
                shell.WriteLine(Loc.GetString("Healing the user's mob since no arguments were provided."));
                if (player.AttachedEntity == null)
                {
                    shell.WriteLine(Loc.GetString("There's no entity attached to the user."));
                    return;
                }
                RejuvenateVerb.PerformRejuvenate(player.AttachedEntity);
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            foreach (var arg in args)
            {
                if(!EntityUid.TryParse(arg, out var uid) || !entityManager.TryGetEntity(uid, out var entity))
                {
                    shell.WriteLine(Loc.GetString("Could not find entity {0}", arg));
                    continue;
                }
                RejuvenateVerb.PerformRejuvenate(entity);
            }
        }
    }
}
