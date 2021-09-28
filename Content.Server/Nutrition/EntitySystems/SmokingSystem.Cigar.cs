using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Interaction;
using Content.Shared.Smoking;
using Content.Shared.Temperature;
using Robust.Shared.GameObjects;

namespace Content.Server.Nutrition.EntitySystems
{
    public partial class SmokingSystem
    {
        private void InitializeCigars()
        {
            SubscribeLocalEvent<CigarComponent, ActivateInWorldEvent>(OnCigarActivatedEvent);
            SubscribeLocalEvent<CigarComponent, InteractUsingEvent>(OnCigarInteractUsingEvent);
            SubscribeLocalEvent<CigarComponent, SmokableSolutionEmptyEvent>(OnCigarSolutionEmptyEvent);
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
            RaiseLocalEvent(args.Used.Uid, isHotEvent, false);

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
