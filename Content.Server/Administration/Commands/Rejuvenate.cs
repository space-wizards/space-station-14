using Content.Server.Damage;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    class Rejuvenate : IConsoleCommand
    {
        public string Command => "rejuvenate";

        public string Description => Loc.GetString("rejuvenate-command-description");

        public string Help => Loc.GetString("rejuvenate-command-help-text");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (args.Length < 1 && player != null) //Try to heal the users mob if applicable
            {
                shell.WriteLine(Loc.GetString("rejuvenate-command-self-heal-message"));
                if (player.AttachedEntity == null)
                {
                    shell.WriteLine(Loc.GetString("rejuvenate-command-no-entity-attached-message"));
                    return;
                }
                RejuvenateVerb.PerformRejuvenate(player.AttachedEntity);
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            foreach (var arg in args)
            {
                if(!EntityUid.TryParse(arg, out var uid) || !entityManager.TryGetEntity(uid, out var entity))
                {
                    shell.WriteLine(Loc.GetString("shell-could-not-find-entity",("entity", arg)));
                    continue;
                }
                RejuvenateVerb.PerformRejuvenate(entity);
            }
        }
    }
}
