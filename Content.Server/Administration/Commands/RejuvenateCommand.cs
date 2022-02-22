using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Damage;
using Content.Shared.Jittering;
using Content.Shared.MobState.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.StatusEffect;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

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
            var targetUid = target;
            var entMan = IoCManager.Resolve<IEntityManager>();
            entMan.GetComponentOrNull<MobStateComponent>(targetUid)?.UpdateState(0);
            entMan.GetComponentOrNull<HungerComponent>(targetUid)?.ResetFood();
            entMan.GetComponentOrNull<ThirstComponent>(targetUid)?.ResetThirst();

            // TODO holy shit make this an event my man!
            EntitySystem.Get<StatusEffectsSystem>().TryRemoveAllStatusEffects(target);

            if (entMan.TryGetComponent(target, out FlammableComponent? flammable))
            {
                EntitySystem.Get<FlammableSystem>().Extinguish(target, flammable);
            }

            if (entMan.TryGetComponent(target, out DamageableComponent? damageable))
            {
                EntitySystem.Get<DamageableSystem>().SetAllDamage(damageable, 0);
            }

            if (entMan.TryGetComponent(target, out CreamPiedComponent? creamPied))
            {
                EntitySystem.Get<CreamPieSystem>().SetCreamPied(target, creamPied, false);
            }

            if (entMan.TryGetComponent(target, out BloodstreamComponent? bloodStream))
            {
                var sys = EntitySystem.Get<BloodstreamSystem>();
                sys.TryModifyBleedAmount(target, -bloodStream.BleedAmount, bloodStream);
                sys.TryModifyBloodLevel(target, bloodStream.BloodSolution.AvailableVolume, bloodStream);
            }

            if (entMan.HasComponent<JitteringComponent>(target))
            {
                entMan.RemoveComponent<JitteringComponent>(target);
            }
        }
    }
}
