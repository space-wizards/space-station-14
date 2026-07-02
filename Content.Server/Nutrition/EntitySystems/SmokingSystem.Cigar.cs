using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Smoking;
using Content.Shared.Temperature;

namespace Content.Server.Nutrition.EntitySystems
{
    public sealed partial class SmokingSystem
    {
        private void InitializeCigars()
        {
            SubscribeLocalEvent<CigarComponent, ActivateInWorldEvent>(OnCigarActivatedEvent);
            SubscribeLocalEvent<CigarComponent, InteractUsingEvent>(OnCigarInteractUsingEvent);
            SubscribeLocalEvent<CigarComponent, SmokableSolutionEmptyEvent>(OnCigarSolutionEmptyEvent);
        }

        private void OnCigarActivatedEvent(Entity<CigarComponent> entity, ref ActivateInWorldEvent args)
        {
            if (args.Handled || !args.Complex)
                return;

            if (!TryComp(entity, out SmokableComponent? smokable))
                return;

            if (smokable.State != SmokableState.Lit)
                return;

            SetSmokableState(entity, SmokableState.Burnt, smokable);
            args.Handled = true;
        }

        private void OnCigarInteractUsingEvent(Entity<CigarComponent> entity, ref InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp(entity, out SmokableComponent? smokable))
                return;

            if (smokable.State != SmokableState.Unlit)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(args.Used, isHotEvent, false);

            if (!isHotEvent.IsHot)
                return;

            SetSmokableState(entity, SmokableState.Lit, smokable);
            args.Handled = true;
        }

        protected override void TryLightCigarFromInteraction(Entity<CigarComponent> entity, SmokableComponent smokable, ref AfterInteractEvent args)
        {
            if (args.Target is not { } target)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(target, isHotEvent, true);

            if (!isHotEvent.IsHot)
                return;

            SetSmokableState(entity, SmokableState.Lit, smokable);
            args.Handled = true;
        }

        private void OnCigarSolutionEmptyEvent(Entity<CigarComponent> entity, ref SmokableSolutionEmptyEvent args)
        {
            SetSmokableState(entity, SmokableState.Burnt);
        }
    }
}
