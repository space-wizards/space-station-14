using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.Chemistry.EntitySystems
{
    public sealed partial class ChemistrySystem
    {
        private void InitializeHypospray()
        {
            SubscribeLocalEvent<HyposprayComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<HyposprayComponent, MeleeHitEvent>(OnAttack);
            SubscribeLocalEvent<HyposprayComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<HyposprayComponent, UseInHandEvent>(OnUseInHand);
        }

        private void OnUseInHand(EntityUid uid, HyposprayComponent component, UseInHandEvent args)
        {
            if (args.Handled) return;

            component.TryDoInject(args.User, args.User);
            args.Handled = true;
        }

        private void OnSolutionChange(EntityUid uid, HyposprayComponent component, SolutionChangedEvent args)
        {
            Dirty(component);
        }

        public void OnAfterInteract(EntityUid uid, HyposprayComponent comp, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            var target = args.Target;
            var user = args.User;

            comp.TryDoInject(target, user);
        }

        public void OnAttack(EntityUid uid, HyposprayComponent comp, MeleeHitEvent args)
        {
            if (!args.HitEntities.Any())
                return;

            comp.TryDoInject(args.HitEntities.First(), args.User);
        }
    }
}
