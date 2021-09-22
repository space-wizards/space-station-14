using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Stunnable.Components;
using Content.Shared.Damage;
using Content.Shared.MobState;
using Content.Shared.Nutrition.Components;
using Content.Shared.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Damage
{
    /// <summary>
    ///     Completely removes all damage from the DamageableComponent (heals the mob).
    /// </summary>
    [GlobalVerb]
    public class RejuvenateVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;
        public override bool BlockedByContainers => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Text = Loc.GetString("rejuvenate-verb-get-data-text");
            data.CategoryData = VerbCategories.Debug;
            data.Visibility = VerbVisibility.Invisible;
            data.IconTexture = "/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png";

            var groupController = IoCManager.Resolve<IConGroupController>();

            if (user.TryGetComponent<ActorComponent>(out var player))
            {
                if (!target.HasComponent<DamageableComponent>() && !target.HasComponent<HungerComponent>() &&
                    !target.HasComponent<ThirstComponent>())
                {
                    return;
                }

                if (groupController.CanCommand(player.PlayerSession, "rejuvenate"))
                {
                    data.Visibility = VerbVisibility.Visible;
                }
            }
        }

        public override void Activate(IEntity user, IEntity target)
        {
            var groupController = IoCManager.Resolve<IConGroupController>();
            if (user.TryGetComponent<ActorComponent>(out var player))
            {
                if (groupController.CanCommand(player.PlayerSession, "rejuvenate"))
                    PerformRejuvenate(target);
            }
        }

        public static void PerformRejuvenate(IEntity target)
        {
            if (target.TryGetComponent(out DamageableComponent? damageable))
            {
                EntitySystem.Get<DamageableSystem>().SetAllDamage(damageable, 0);
            }

            if (target.TryGetComponent(out HungerComponent? hunger))
            {
                hunger.ResetFood();
            }

            if (target.TryGetComponent(out ThirstComponent? thirst))
            {
                thirst.ResetThirst();
            }

            if (target.TryGetComponent(out StunnableComponent? stun))
            {
                stun.ResetStuns();
            }

            if (target.TryGetComponent(out FlammableComponent? flammable))
            {
                EntitySystem.Get<FlammableSystem>().Extinguish(target.Uid, flammable);
            }

            if (target.TryGetComponent(out CreamPiedComponent? creamPied))
            {
                EntitySystem.Get<CreamPieSystem>().SetCreamPied(target.Uid, creamPied, false);
            }
        }
    }
}
