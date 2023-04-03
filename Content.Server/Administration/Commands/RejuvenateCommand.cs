using Content.Server.Atmos.Miasma;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Disease.Components;
using Content.Server.Disease;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Damage;
using Content.Shared.Jittering;
using Content.Shared.Nutrition.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffect;
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
