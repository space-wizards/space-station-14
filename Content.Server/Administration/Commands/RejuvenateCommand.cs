using Content.Shared.Administration;
using Content.Shared.Rejuvenate;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class RejuvenateCommand : IConsoleCommand
    {
        public string Command => "rejuvenate";

        public string Description => Loc.GetString("rejuvenate-command-description");

        public string Help => Loc.GetString("rejuvenate-command-help-text");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1 && shell.Player is IPlayerSession player) //Try to heal the users mob if applicable
            {
                shell.WriteLine(Loc.GetString("rejuvenate-command-self-heal-message"));
                if (player.AttachedEntity == null)
                {
                    shell.WriteLine(Loc.GetString("rejuvenate-command-no-entity-attached-message"));
                    return;
                }
                PerformRejuvenate(player.AttachedEntity.Value);
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            foreach (var arg in args)
            {
                if (!EntityUid.TryParse(arg, out var entity) || !entityManager.EntityExists(entity))
                {
                    shell.WriteLine(Loc.GetString("shell-could-not-find-entity",("entity", arg)));
                    continue;
                }
                PerformRejuvenate(entity);
            }
        }

        public static void PerformRejuvenate(EntityUid target)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            entityManager.EventBus.RaiseLocalEvent(target, new RejuvenateEvent());
        }
    }
}
