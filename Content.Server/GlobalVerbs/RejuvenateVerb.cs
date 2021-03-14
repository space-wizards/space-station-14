using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GlobalVerbs
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
            data.Text = Loc.GetString("Rejuvenate");
            data.CategoryData = VerbCategories.Debug;
            data.Visibility = VerbVisibility.Invisible;
            data.IconTexture = "/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png";

            var groupController = IoCManager.Resolve<IConGroupController>();

            if (user.TryGetComponent<IActorComponent>(out var player))
            {
                if (!target.HasComponent<IDamageableComponent>() && !target.HasComponent<HungerComponent>() &&
                    !target.HasComponent<ThirstComponent>())
                {
                    return;
                }

                if (groupController.CanCommand(player.playerSession, "rejuvenate"))
                {
                    data.Visibility = VerbVisibility.Visible;
                }
            }
        }

        public override void Activate(IEntity user, IEntity target)
        {
            var groupController = IoCManager.Resolve<IConGroupController>();
            if (user.TryGetComponent<IActorComponent>(out var player))
            {
                if (groupController.CanCommand(player.playerSession, "rejuvenate"))
                    PerformRejuvenate(target);
            }
        }

        public static void PerformRejuvenate(IEntity target)
        {
            if (target.TryGetComponent(out IDamageableComponent damage))
            {
                damage.Heal();
            }

            if (target.TryGetComponent(out IMobStateComponent mobState))
            {
                mobState.UpdateState(0);
            }

            if (target.TryGetComponent(out HungerComponent hunger))
            {
                hunger.ResetFood();
            }

            if (target.TryGetComponent(out ThirstComponent thirst))
            {
                thirst.ResetThirst();
            }

            if (target.TryGetComponent(out StunnableComponent stun))
            {
                stun.ResetStuns();
            }

            if (target.TryGetComponent(out FlammableComponent flammable))
            {
                flammable.Extinguish();
            }

            if (target.TryGetComponent(out CreamPiedComponent creamPied))
            {
                creamPied.Wash();
            }
        }
    }
}
