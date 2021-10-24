using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Stunnable;
using Content.Server.Stunnable.Components;
using Content.Shared.Administration;
using Content.Shared.Damage;
using Content.Shared.Jittering;
using Content.Shared.MobState;
using Content.Shared.Nutrition.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public class RejuvenateCommand : IConsoleCommand
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
                PerformRejuvenate(player.AttachedEntity);
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            foreach (var arg in args)
            {
                if(!EntityUid.TryParse(arg, out var uid) || !entityManager.TryGetEntity(uid, out var entity))
                {
                    shell.WriteLine(Loc.GetString("shell-could-not-find-entity",("entity", arg)));
                    continue;
                }
                PerformRejuvenate(entity);
            }
        }

        public static void PerformRejuvenate(IEntity target)
        {
            target.GetComponentOrNull<IMobStateComponent>()?.UpdateState(0);
            target.GetComponentOrNull<HungerComponent>()?.ResetFood();
            target.GetComponentOrNull<ThirstComponent>()?.ResetThirst();

            EntitySystem.Get<StatusEffectsSystem>().TryRemoveAllStatusEffects(target.Uid);

            if (target.TryGetComponent(out FlammableComponent? flammable))
            {
                EntitySystem.Get<FlammableSystem>().Extinguish(target.Uid, flammable);
            }

            if (target.TryGetComponent(out DamageableComponent? damageable))
            {
                EntitySystem.Get<DamageableSystem>().SetAllDamage(damageable, 0);
            }

            if (target.TryGetComponent(out CreamPiedComponent? creamPied))
            {
                EntitySystem.Get<CreamPieSystem>().SetCreamPied(target.Uid, creamPied, false);
            }

            if (target.HasComponent<JitteringComponent>())
            {
                target.RemoveComponent<JitteringComponent>();
            }
        }
    }
}
