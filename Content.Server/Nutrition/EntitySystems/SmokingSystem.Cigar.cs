using Content.Server.Nutrition.Components;
using Content.Shared.Interaction;
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
            SubscribeLocalEvent<CigarComponent, AfterInteractEvent>(OnCigarAfterInteract);
        }

        private void OnCigarActivatedEvent(EntityUid uid, CigarComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            if (!EntityManager.TryGetComponent(uid, out SmokableComponent? smokable))
                return;

            if (smokable.State != SmokableState.Lit)
                return;

            SetSmokableState(uid, SmokableState.Burnt, smokable);
            args.Handled = true;
        }

        private void OnCigarInteractUsingEvent(EntityUid uid, CigarComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!EntityManager.TryGetComponent(uid, out SmokableComponent? smokable))
                return;

            if (smokable.State != SmokableState.Unlit)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(args.Used, isHotEvent, false);

            if (!isHotEvent.IsHot)
                return;

            SetSmokableState(uid, SmokableState.Lit, smokable);
            args.Handled = true;
        }

        public void OnCigarAfterInteract(EntityUid uid, CigarComponent component, AfterInteractEvent args)
        {
            var targetEntity = args.Target;
            if (targetEntity == null ||
                !args.CanReach ||
                !EntityManager.TryGetComponent(uid, out SmokableComponent? smokable) ||
                smokable.State == SmokableState.Lit)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(targetEntity.Value, isHotEvent, true);

            if (!isHotEvent.IsHot)
                return;

            SetSmokableState(uid, SmokableState.Lit, smokable);
            args.Handled = true;
        }

        private void OnCigarSolutionEmptyEvent(EntityUid uid, CigarComponent component, SmokableSolutionEmptyEvent args)
        {
            SetSmokableState(uid, SmokableState.Burnt);
        }
    }
}
